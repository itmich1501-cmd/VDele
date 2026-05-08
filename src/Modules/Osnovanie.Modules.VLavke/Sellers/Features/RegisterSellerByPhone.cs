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
    string Password,
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
            .WithError(SellerValidationErrors.PhoneIsEmpty());

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(SellerValidationErrors.PasswordIsEmpty())
            .MinimumLength(6)
            .WithError(SellerValidationErrors.PasswordIsTooShort());

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(SellerValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(SellerValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.MainCityId)
            .NotEmpty()
            .WithError(SellerValidationErrors.MainCityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(SellerValidationErrors.EmailIsInvalid());
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

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterSellerByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly ISellerProfileRepository _profileRepository;
    private readonly ISellersReadDbContext _sellersReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterSellerByPhoneRequest> _validator;
    private readonly ILogger<RegisterSellerByPhoneHandler> _logger;

    public RegisterSellerByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        ISellerProfileRepository profileRepository,
        ISellersReadDbContext sellersReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterSellerByPhoneRequest> validator,
        ILogger<RegisterSellerByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _sellersReadDbContext = sellersReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterSellerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return SellerValidationErrors.RequestIsEmpty().ToErrors();

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
                ApplicationCodes.VLavke,
                RoleCodes.Seller),
            cancellationToken);

        if (authResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return authResult.Error;
        }

        var userId = authResult.Value;

        var profileExists = await _sellersReadDbContext.SellerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return SellerErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = SellerProfile.Create(
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

        return userId;
    }
}