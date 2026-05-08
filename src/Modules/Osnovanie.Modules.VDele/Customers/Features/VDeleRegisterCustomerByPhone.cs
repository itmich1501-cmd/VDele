using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.Domain;
using Osnovanie.Modules.VDele.Customers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Customers.Features;

public sealed record RegisterVDeleCustomerByPhoneRequest(
    string Phone,
    string Password,
    string FullName,
    Guid CityId,
    string? Email);

public sealed class RegisterVDeleCustomerByPhoneValidator
    : AbstractValidator<RegisterVDeleCustomerByPhoneRequest>
{
    public RegisterVDeleCustomerByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(VDeleCustomerValidationErrors.PhoneIsEmpty());

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(VDeleCustomerValidationErrors.PasswordIsEmpty())
            .MinimumLength(6)
            .WithError(VDeleCustomerValidationErrors.PasswordIsTooShort());

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(VDeleCustomerValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(VDeleCustomerValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithError(VDeleCustomerValidationErrors.CityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(VDeleCustomerValidationErrors.EmailIsInvalid());
    }
}
public sealed class RegisterVDeleCustomerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vdele/customers/register-by-phone", async (
            [FromServices] RegisterVDeleCustomerByPhoneHandler handler,
            [FromBody] RegisterVDeleCustomerByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterVDeleCustomerByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly IVDeleCustomerProfileRepository _profileRepository;
    private readonly IVDeleCustomersReadDbContext _ivDeleCustomersReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterVDeleCustomerByPhoneRequest> _validator;
    private readonly ILogger<RegisterVDeleCustomerByPhoneHandler> _logger;

    public RegisterVDeleCustomerByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        IVDeleCustomerProfileRepository profileRepository,
        IVDeleCustomersReadDbContext ivDeleCustomersReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterVDeleCustomerByPhoneRequest> validator,
        ILogger<RegisterVDeleCustomerByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _ivDeleCustomersReadDbContext = ivDeleCustomersReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterVDeleCustomerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return VDeleCustomerValidationErrors.RequestIsEmpty().ToErrors();

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

        var profileExists = await _ivDeleCustomersReadDbContext.CustomerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return VDeleCustomerErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = VDeleCustomerProfile.Create(
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