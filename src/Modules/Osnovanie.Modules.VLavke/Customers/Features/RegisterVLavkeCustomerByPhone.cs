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

public sealed record RegisterVLavkeCustomerByPhoneRequest(
    string Phone,
    string Code,
    string FullName,
    Guid CityId,
    string? Email);

public sealed class RegisterVLavkeCustomerByPhoneValidator
    : AbstractValidator<RegisterVLavkeCustomerByPhoneRequest>
{
    public RegisterVLavkeCustomerByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(VLavkeCustomerValidationErrors.PhoneIsEmpty());
        
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithError(VLavkeCustomerValidationErrors.CodeIsInvalid());

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(VLavkeCustomerValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(VLavkeCustomerValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithError(VLavkeCustomerValidationErrors.CityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(VLavkeCustomerValidationErrors.EmailIsInvalid());
    }
}
public sealed class RegisterVLavkeCustomerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vlavke/customers/register-by-phone", async (
            [FromServices] RegisterVLavkeCustomerByPhoneHandler handler,
            [FromBody] RegisterVLavkeCustomerByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<string>(result);
        });
    }
}

public sealed class RegisterVLavkeCustomerByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly IVLavkeCustomerProfileRepository _profileRepository;
    private readonly IVLavkeCustomersReadDbContext _ivLavkeCustomersReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterVLavkeCustomerByPhoneRequest> _validator;
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<RegisterVLavkeCustomerByPhoneHandler> _logger;

    public RegisterVLavkeCustomerByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        IVLavkeCustomerProfileRepository profileRepository,
        IVLavkeCustomersReadDbContext ivLavkeCustomersReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterVLavkeCustomerByPhoneRequest> validator,
        IAuthTokenService authTokenService,
        ILogger<RegisterVLavkeCustomerByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _ivLavkeCustomersReadDbContext = ivLavkeCustomersReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _authTokenService = authTokenService;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(
        RegisterVLavkeCustomerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return VLavkeCustomerValidationErrors.RequestIsEmpty().ToErrors();

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
                request.Code,
                null,
                request.Email,
                ApplicationCodes.VLavke,
                new[] { RoleCodes.Customer }),
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
            return VLavkeCustomerErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = VLavkeCustomerProfile.Create(
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
            "VLavke customer registered. UserId: {UserId}",
            userId);

        var tokenResult = await _authTokenService.GenerateTokenForUser(userId, ApplicationCodes.VLavke, cancellationToken);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        return tokenResult.Value;
    }
}