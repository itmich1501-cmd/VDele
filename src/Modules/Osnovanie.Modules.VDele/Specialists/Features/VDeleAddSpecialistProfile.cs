using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public sealed record AddSpecialistProfileRequest(
    string FullName,
    Guid CityId,
    string? Email,
    string? About);

public sealed class AddSpecialistProfileValidator : AbstractValidator<AddSpecialistProfileRequest>
{
    public AddSpecialistProfileValidator()
    {
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

public sealed class AddSpecialistProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vdele/specialists/profile", async (
                ClaimsPrincipal currentUser,
                [FromServices] AddSpecialistProfileHandler handler,
                [FromBody] AddSpecialistProfileRequest request,
                CancellationToken cancellationToken) =>
            {
                var userIdString = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                var result = await handler.Handle(userId, request, cancellationToken);
                return (IResult)new EndpointResult<string>(result);
            })
            .RequireAuthorization();
    }
}

public sealed class AddSpecialistProfileHandler
{
    private readonly IAuthRegistrationService _authRegistrationService;
    private readonly IAuthTokenService _authTokenService;
    private readonly IVDeleSpecialistProfileRepository _profileRepository;
    private readonly IVDeleSpecialistsReadDbContext _specialistsReadDb;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<AddSpecialistProfileRequest> _validator;
    private readonly ILogger<AddSpecialistProfileHandler> _logger;

    public AddSpecialistProfileHandler(
        IAuthRegistrationService authRegistrationService,
        IAuthTokenService authTokenService,
        IVDeleSpecialistProfileRepository profileRepository,
        IVDeleSpecialistsReadDbContext specialistsReadDb,
        ITransactionManager transactionManager,
        IValidator<AddSpecialistProfileRequest> validator,
        ILogger<AddSpecialistProfileHandler> logger)
    {
        _authRegistrationService = authRegistrationService;
        _authTokenService = authTokenService;
        _profileRepository = profileRepository;
        _specialistsReadDb = specialistsReadDb;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(
        Guid userId,
        AddSpecialistProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error!.ToErrors();

        await using var transaction = transactionResult.Value!;

        var profileExists = await _specialistsReadDb.SpecialistProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        if (profileExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return VDeleSpecialistErrors.AlreadyExists(userId).ToErrors();
        }

        var addRoleResult = await _authRegistrationService.AddRolesToUser(
            userId,
            ApplicationCodes.VDele,
            new[] { RoleCodes.Specialist },
            cancellationToken);

        if (addRoleResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return addRoleResult.Error;
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

        await _profileRepository.Add(profileResult.Value!, cancellationToken);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return saveResult.Error!.ToErrors();
        }

        var commitResult = await transaction.CommitAsync(cancellationToken);
        if (commitResult.IsFailure)
            return commitResult.Error!.ToErrors();

        _logger.LogInformation(
            "Specialist profile added to existing user. UserId: {UserId}",
            userId);

        return await _authTokenService.GenerateTokenForUser(
            userId,
            ApplicationCodes.VDele,
            cancellationToken);
    }
}
