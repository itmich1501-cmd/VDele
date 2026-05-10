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
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VDele.Specialists.Domain;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public sealed record RegisterSpecialistByPhoneRequest(
    string Phone,
    string Code,
    string FullName,
    Guid CityId,
    string? Email,
    string? About);

public sealed class VDeleRegisterSpecialistByPhoneValidator
    : AbstractValidator<RegisterSpecialistByPhoneRequest>
{
    public VDeleRegisterSpecialistByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(VDeleSpecialistValidationErrors.PhoneIsEmpty());
        
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithError(VDeleSpecialistValidationErrors.CodeIsInvalid());
        
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(VDeleSpecialistValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(VDeleSpecialistValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithError(VDeleSpecialistValidationErrors.CityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(VDeleSpecialistValidationErrors.EmailIsInvalid());

        RuleFor(x => x.About)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.About))
            .WithError(VDeleSpecialistValidationErrors.AboutIsTooLong());
    }
}

public sealed class VDeleRegisterSpecialistByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vdele/specialists/register-by-phone", async (
            [FromServices] VDeleRegisterVDeleSpecialistByPhoneHandler handler,
            [FromBody] RegisterSpecialistByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<string>(result);
        });
    }
}

public sealed class VDeleRegisterVDeleSpecialistByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly IVDeleSpecialistProfileRepository _profileRepository;
    private readonly IVDeleSpecialistsReadDbContext _ivDeleSpecialistsReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterSpecialistByPhoneRequest> _validator;
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<VDeleRegisterVDeleSpecialistByPhoneHandler> _logger;

    public VDeleRegisterVDeleSpecialistByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        IVDeleSpecialistProfileRepository profileRepository,
        IVDeleSpecialistsReadDbContext ivDeleSpecialistsReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterSpecialistByPhoneRequest> validator,
        IAuthTokenService authTokenService,
        ILogger<VDeleRegisterVDeleSpecialistByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _ivDeleSpecialistsReadDbContext = ivDeleSpecialistsReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _authTokenService = authTokenService;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(
        RegisterSpecialistByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return VDeleSpecialistValidationErrors.RequestIsEmpty().ToErrors();

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
                ApplicationCodes.VDele,
                RoleCodes.Specialist),
            cancellationToken);

        if (authResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return authResult.Error;
        }

        var userId = authResult.Value;

        var profileExists = await _ivDeleSpecialistsReadDbContext.SpecialistProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return VDeleSpecialistErrors.AlreadyExists(userId).ToErrors();
        }

        var profileResult = VDeleSpecialistProfile.Create(
            userId,
            request.FullName,
            request.CityId,
            request.Email,
            request.About);

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
            "VDele specialist registered. UserId: {UserId}",
            userId);

        var tokenResult = await _authTokenService.GenerateTokenForUser(userId, cancellationToken);
        if (tokenResult.IsFailure)
            return tokenResult.Error;

        return tokenResult.Value;
    }
}