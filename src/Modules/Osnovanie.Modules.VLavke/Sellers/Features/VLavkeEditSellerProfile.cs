using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Sellers.Features;

public sealed record VLavkeEditSellerProfileRequest(
    string FullName,
    Guid MainCityId,
    string? Email);

public sealed class VLavkeEditSellerProfileValidator : AbstractValidator<VLavkeEditSellerProfileRequest>
{
    public VLavkeEditSellerProfileValidator()
    {
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

public sealed class VLavkeEditSellerProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("vlavke/sellers/me", async Task<EndpointResult<VLavkeGetSellerMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VLavkeEditSellerProfileHandler handler,
                [FromBody] VLavkeEditSellerProfileRequest request,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, request, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VLavkeEditSellerProfileHandler
{
    private readonly IVLavkeSellerProfileRepository _profileRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<VLavkeEditSellerProfileRequest> _validator;
    private readonly ILogger<VLavkeEditSellerProfileHandler> _logger;

    public VLavkeEditSellerProfileHandler(
        IVLavkeSellerProfileRepository profileRepository,
        ITransactionManager transactionManager,
        IValidator<VLavkeEditSellerProfileRequest> validator,
        ILogger<VLavkeEditSellerProfileHandler> logger)
    {
        _profileRepository = profileRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VLavkeGetSellerMeResponse, Errors>> Handle(
        Guid userId,
        VLavkeEditSellerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var profile = await _profileRepository.GetByUserId(userId, cancellationToken);
        if (profile is null)
            return VLavkeSellerErrors.ProfileNotFound(userId).ToErrors();

        var updateResult = profile.Update(request.FullName, request.MainCityId, request.Email);
        if (updateResult.IsFailure)
            return updateResult.Error!.ToErrors();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error!.ToErrors();

        _logger.LogInformation(
            "VLavke seller profile updated. UserId: {UserId}",
            userId);

        return new VLavkeGetSellerMeResponse(
            profile.UserId,
            profile.FullName,
            profile.MainCityId,
            profile.Email,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
