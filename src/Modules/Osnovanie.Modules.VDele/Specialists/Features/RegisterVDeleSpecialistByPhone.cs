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
using Osnovanie.Modules.Auth.Services;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VDele.Specialists.Domain;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public sealed record RegisterSpecialistByPhoneRequest(
    string Phone,
    string Password,
    string FullName,
    Guid CityId,
    string? Email,
    string? About);

public sealed class RegisterSpecialistByPhoneValidator
    : AbstractValidator<RegisterSpecialistByPhoneRequest>
{
    public RegisterSpecialistByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(SpecialistValidationErrors.PhoneIsEmpty());

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(SpecialistValidationErrors.PasswordIsEmpty())
            .MinimumLength(6)
            .WithError(SpecialistValidationErrors.PasswordIsTooShort());

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(SpecialistValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(SpecialistValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithError(SpecialistValidationErrors.CityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(SpecialistValidationErrors.EmailIsInvalid());

        RuleFor(x => x.About)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.About))
            .WithError(SpecialistValidationErrors.AboutIsTooLong());
    }
}

public sealed class RegisterSpecialistByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vdele/specialists/register-by-phone", async (
            [FromServices] RegisterVDeleSpecialistByPhoneHandler handler,
            [FromBody] RegisterSpecialistByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterVDeleSpecialistByPhoneHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly IVDeleSpecialistProfileRepository _profileRepository;
    private readonly IVDeleSpecialistsReadDbContext _ivDeleSpecialistsReadDbContext;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<RegisterSpecialistByPhoneRequest> _validator;
    private readonly ILogger<RegisterVDeleSpecialistByPhoneHandler> _logger;

    public RegisterVDeleSpecialistByPhoneHandler(
        IAuthRegistrationService authRegistrationService,
        IVDeleSpecialistProfileRepository profileRepository,
        IVDeleSpecialistsReadDbContext ivDeleSpecialistsReadDbContext,
        ITransactionManager transactionManager,
        IValidator<RegisterSpecialistByPhoneRequest> validator,
        ILogger<RegisterVDeleSpecialistByPhoneHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _profileRepository = profileRepository;
        _ivDeleSpecialistsReadDbContext = ivDeleSpecialistsReadDbContext;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterSpecialistByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return SpecialistValidationErrors.RequestIsEmpty().ToErrors();

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

        return userId;
    }
}