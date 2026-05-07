using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.Domain;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.VDele.Customers.Features;

public sealed record RegisterCustomerByPhoneRequest(
    string Phone,
    string Password,
    string FullName,
    Guid CityId,
    string? Email);

public sealed class RegisterCustomerByPhoneValidator
    : AbstractValidator<RegisterCustomerByPhoneRequest>
{
    public RegisterCustomerByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Телефон обязателен");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .WithMessage("Пароль должен содержать минимум 6 символов");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("ФИО обязательно");

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithMessage("Город обязателен");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email имеет неверный формат");
    }
}

public sealed class RegisterVDeleCustomerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/vdele/customer/register-by-phone", async (
            [FromServices] RegisterCustomerByPhoneHandler handler,
            [FromBody] RegisterCustomerByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterCustomerByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly ICustomerProfileRepository _profileRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterCustomerByPhoneRequest> _validator;
    private readonly ILogger<RegisterCustomerByPhoneHandler> _logger;

    public RegisterCustomerByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        ICustomerProfileRepository profileRepository,
        ITransactionManager transactionManager,
        IValidator<RegisterCustomerByPhoneRequest> validator,
        ILogger<RegisterCustomerByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterCustomerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Error.Validation(
                "vdele.customer.register.request.empty",
                "Тело запроса обязательно",
                "request").ToErrors();
        }

        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var transactionResult = await _transactionManager.BeginTransactionAsync(
            cancellationToken);

        if (transactionResult.IsFailure)
            return transactionResult.Error!.ToErrors();

        await using var transaction = transactionResult.Value!;

        var authResult = await _authRegistrationService.RegisterByPhone(
            new RegisterUserByPhoneCommand(
                request.Phone,
                request.Password,
                request.Email,
                ApplicationCodes.VDele,
                RoleCodes.Customer),
            cancellationToken);

        if (authResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return authResult.Error;
        }

        var userId = authResult.Value;

        var profileExists = await _profileRepository.ExistsByUserId(
            userId,
            cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CustomerErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = CustomerProfile.Create(
            userId,
            request.FullName,
            request.CityId,
            request.Email);

        if (profileResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return profileResult.Error!.ToErrors();
        }

        await _profileRepository.Add(
            profileResult.Value!,
            cancellationToken);

        var saveResult = await _transactionManager.SaveChangesAsync(
            cancellationToken);

        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return saveResult.Error!.ToErrors();
        }

        var commitResult = await transaction.CommitAsync(cancellationToken);

        if (commitResult.IsFailure)
            return commitResult.Error!.ToErrors();

        _logger.LogInformation(
            "VDele customer registered. UserId: {UserId}",
            userId);

        return userId;
    }
}