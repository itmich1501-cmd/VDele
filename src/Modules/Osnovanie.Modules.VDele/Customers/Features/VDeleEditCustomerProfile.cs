using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Customers.Features;

public sealed record EditCustomerProfileRequest(
    string FullName,
    Guid CityId,
    string? Email);

public sealed class EditCustomerProfileValidator : AbstractValidator<EditCustomerProfileRequest>
{
    public EditCustomerProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithError(VDeleCustomerValidationErrors.FullNameIsEmpty())
            .MaximumLength(200)
            .WithError(VDeleCustomerValidationErrors.FullNameIsTooLong());

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithError(VDeleCustomerValidationErrors.CityIdIsEmpty());

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithError(VDeleCustomerValidationErrors.EmailIsInvalid());
    }
}

public sealed class VDeleEditCustomerProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("vdele/customers/me", async Task<EndpointResult<VDeleGetCustomerMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VDeleEditCustomerProfileHandler handler,
                [FromBody] EditCustomerProfileRequest request,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, request, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VDeleEditCustomerProfileHandler
{
    private readonly IVDeleCustomerProfileRepository _profileRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<EditCustomerProfileRequest> _validator;
    private readonly ILogger<VDeleEditCustomerProfileHandler> _logger;

    public VDeleEditCustomerProfileHandler(
        IVDeleCustomerProfileRepository profileRepository,
        ITransactionManager transactionManager,
        IValidator<EditCustomerProfileRequest> validator,
        ILogger<VDeleEditCustomerProfileHandler> logger)
    {
        _profileRepository = profileRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VDeleGetCustomerMeResponse, Errors>> Handle(
        Guid userId,
        EditCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var profile = await _profileRepository.GetByUserId(userId, cancellationToken);
        if (profile is null)
            return VDeleCustomerErrors.ProfileNotFound(userId).ToErrors();

        var updateResult = profile.Update(request.FullName, request.CityId, request.Email);
        if (updateResult.IsFailure)
            return updateResult.Error!.ToErrors();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error!.ToErrors();

        _logger.LogInformation(
            "VDele customer profile updated. UserId: {UserId}",
            userId);

        return new VDeleGetCustomerMeResponse(
            profile.UserId,
            profile.FullName,
            profile.CityId,
            profile.Email,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
