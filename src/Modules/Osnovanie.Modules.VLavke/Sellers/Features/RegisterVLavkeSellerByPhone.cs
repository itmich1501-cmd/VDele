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
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.Domain;
using Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Sellers.Features;

public sealed record RegisterSellerByPhoneRequest(
    string Phone,
    string Code,
    string FullName,
    Guid MainCityId,
    string? Email);

public sealed class RegisterSellerByPhoneValidator
    : AbstractValidator<RegisterSellerByPhoneRequest>
{
    public RegisterSellerByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(VLavkeSellerValidationErrors.PhoneIsEmpty());
        
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithError(VLavkeSellerValidationErrors.CodeIsInvalid());
        
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(VLavkeSellerValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(VLavkeSellerValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.MainCityId)
            .NotEmpty()
            .WithError(VLavkeSellerValidationErrors.MainCityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(VLavkeSellerValidationErrors.EmailIsInvalid());
    }
}

public sealed class RegisterSellerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vlavke/sellers/register-by-phone", async (
            [FromServices] RegisterSellerByPhoneHandler handler,
            [FromBody] RegisterSellerByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<string>(result);
        });
    }
}

public sealed class RegisterSellerByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly IVLavkeSellerProfileRepository _profileRepository;
    private readonly IVLavkeSellersReadDbContext _ivLavkeSellersReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterSellerByPhoneRequest> _validator;
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<RegisterSellerByPhoneHandler> _logger;

    public RegisterSellerByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        IVLavkeSellerProfileRepository profileRepository,
        IVLavkeSellersReadDbContext ivLavkeSellersReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterSellerByPhoneRequest> validator,
        IAuthTokenService authTokenService,
        ILogger<RegisterSellerByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _ivLavkeSellersReadDbContext = ivLavkeSellersReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _authTokenService = authTokenService;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(
        RegisterSellerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return VLavkeSellerValidationErrors.RequestIsEmpty().ToErrors();

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
                RoleCodes.Seller),
            cancellationToken);

        if (authResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return authResult.Error;
        }

        var userId = authResult.Value;

        var profileExists = await _ivLavkeSellersReadDbContext.SellerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return VLavkeSellerErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = VLavkeSellerProfile.Create(
            userId,
            request.FullName,
            request.MainCityId,
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
            "VLavke seller registered. UserId: {UserId}",
            userId);

        var tokenResult = await _authTokenService.GenerateTokenForUser(userId, cancellationToken);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        return tokenResult.Value;
    }
}