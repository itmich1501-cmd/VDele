using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Customers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Customers.Features;

public sealed record VLavkeEditCustomerProfileRequest(
    string FullName,
    Guid CityId,
    string? Email);

public sealed class VLavkeEditCustomerProfileValidator : AbstractValidator<VLavkeEditCustomerProfileRequest>
{
    public VLavkeEditCustomerProfileValidator()
    {
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

public sealed class VLavkeEditCustomerProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("vlavke/customers/me", async Task<EndpointResult<VLavkeGetCustomerMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VLavkeEditCustomerProfileHandler handler,
                [FromBody] VLavkeEditCustomerProfileRequest request,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, request, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VLavkeEditCustomerProfileHandler
{
    private readonly IVLavkeCustomerProfileRepository _profileRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<VLavkeEditCustomerProfileRequest> _validator;
    private readonly ILogger<VLavkeEditCustomerProfileHandler> _logger;

    public VLavkeEditCustomerProfileHandler(
        IVLavkeCustomerProfileRepository profileRepository,
        ITransactionManager transactionManager,
        IValidator<VLavkeEditCustomerProfileRequest> validator,
        ILogger<VLavkeEditCustomerProfileHandler> logger)
    {
        _profileRepository = profileRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VLavkeGetCustomerMeResponse, Errors>> Handle(
        Guid userId,
        VLavkeEditCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var profile = await _profileRepository.GetByUserId(userId, cancellationToken);
        if (profile is null)
            return VLavkeCustomerErrors.ProfileNotFound(userId).ToErrors();

        var updateResult = profile.Update(request.FullName, request.CityId, request.Email);
        if (updateResult.IsFailure)
            return updateResult.Error!.ToErrors();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error!.ToErrors();

        _logger.LogInformation(
            "VLavke customer profile updated. UserId: {UserId}",
            userId);

        return new VLavkeGetCustomerMeResponse(
            profile.UserId,
            profile.FullName,
            profile.CityId,
            profile.Email,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
