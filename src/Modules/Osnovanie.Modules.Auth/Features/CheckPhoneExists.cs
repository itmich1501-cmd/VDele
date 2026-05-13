using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.Auth.Features;

public record CheckPhoneExistsRequest(string Phone);

public class CheckPhoneExistsRequestValidator : AbstractValidator<CheckPhoneExistsRequest>
{
    public CheckPhoneExistsRequestValidator()
    {
        RuleFor(x => x.Phone).Phone();
    }
}

public class CheckPhoneExists : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("auth/phone/exists", async Task<EndpointResult> (
            CheckPhoneExistsHandler handler,
            string phone,
            CancellationToken cancellationToken
        ) =>
        {
            return await handler.Handle(new CheckPhoneExistsRequest(phone), cancellationToken);
        });
    }
}

public class CheckPhoneExistsHandler
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<CheckPhoneExistsRequest> _validator;

    public CheckPhoneExistsHandler(
        UserManager<User> userManager,
        IValidator<CheckPhoneExistsRequest> validator)
    {
        _userManager = userManager;
        _validator = validator;
    }

    public async Task<UnitResult<Errors>> Handle(
        CheckPhoneExistsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var exists = await _userManager.Users
            .AnyAsync(u => u.PhoneNumber == request.Phone, cancellationToken);

        if (!exists)
            return Error.NotFound(
                "auth.user.not_found_by_phone",
                "Пользователь с таким телефоном не найден").ToErrors();

        return UnitResult.Success<Errors>();
    }
}
