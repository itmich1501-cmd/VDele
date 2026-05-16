using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public sealed record EditSpecialistProfileRequest(
    string FullName,
    Guid CityId,
    string? Email,
    string? About);

public sealed class EditSpecialistProfileValidator : AbstractValidator<EditSpecialistProfileRequest>
{
    public EditSpecialistProfileValidator()
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

public sealed class VDeleEditSpecialistProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("vdele/specialists/me", async Task<EndpointResult<VDeleGetSpecialistMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VDeleEditSpecialistProfileHandler handler,
                [FromBody] EditSpecialistProfileRequest request,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, request, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VDeleEditSpecialistProfileHandler
{
    private readonly IVDeleSpecialistProfileRepository _profileRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<EditSpecialistProfileRequest> _validator;
    private readonly ILogger<VDeleEditSpecialistProfileHandler> _logger;

    public VDeleEditSpecialistProfileHandler(
        IVDeleSpecialistProfileRepository profileRepository,
        ITransactionManager transactionManager,
        IValidator<EditSpecialistProfileRequest> validator,
        ILogger<VDeleEditSpecialistProfileHandler> logger)
    {
        _profileRepository = profileRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VDeleGetSpecialistMeResponse, Errors>> Handle(
        Guid userId,
        EditSpecialistProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var profile = await _profileRepository.GetByUserId(userId, cancellationToken);
        if (profile is null)
            return VDeleSpecialistErrors.ProfileNotFound(userId).ToErrors();

        var updateResult = profile.Update(request.FullName, request.CityId, request.Email, request.About);
        if (updateResult.IsFailure)
            return updateResult.Error!.ToErrors();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error!.ToErrors();

        _logger.LogInformation(
            "VDele specialist profile updated. UserId: {UserId}",
            userId);

        return new VDeleGetSpecialistMeResponse(
            profile.UserId,
            profile.FullName,
            profile.CityId,
            profile.Email,
            profile.About,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
