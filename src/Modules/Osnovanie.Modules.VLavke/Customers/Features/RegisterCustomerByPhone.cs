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
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Customers.Domain;
using Osnovanie.Modules.VLavke.Customers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Customers.Features;

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
            .WithError(CustomerValidationErrors.PhoneIsEmpty());

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(CustomerValidationErrors.PasswordIsEmpty())
            .MinimumLength(6)
            .WithError(CustomerValidationErrors.PasswordIsTooShort());

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(CustomerValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(CustomerValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithError(CustomerValidationErrors.CityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(CustomerValidationErrors.EmailIsInvalid());
    }
}
public sealed class RegisterVDeleCustomerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vdele/customers/register-by-phone", async (
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
    private readonly IVLavkeCustomerProfileRepository _profileRepository;
    private readonly IVLavkeCustomersReadDbContext _ivLavkeCustomersReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterCustomerByPhoneRequest> _validator;
    private readonly ILogger<RegisterCustomerByPhoneHandler> _logger;

    public RegisterCustomerByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        IVLavkeCustomerProfileRepository profileRepository,
        IVLavkeCustomersReadDbContext ivLavkeCustomersReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterCustomerByPhoneRequest> validator,
        ILogger<RegisterCustomerByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _ivLavkeCustomersReadDbContext = ivLavkeCustomersReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterCustomerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return CustomerValidationErrors.RequestIsEmpty().ToErrors();

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

        var profileExists = await _ivLavkeCustomersReadDbContext.CustomerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CustomerErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = IVLavkeCustomerProfile.Create(
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