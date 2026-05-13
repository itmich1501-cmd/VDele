using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.Auth.Features;

public record CheckPhoneExistsRequest(string Phone, string ApplicationCode, string RoleCode);

public class CheckPhoneExistsRequestValidator : AbstractValidator<CheckPhoneExistsRequest>
{
    public CheckPhoneExistsRequestValidator()
    {
        RuleFor(x => x.Phone).Phone();

        RuleFor(x => x.ApplicationCode)
            .NotEmpty()
            .WithError(Error.Validation(
                "auth.application.empty",
                "ApplicationCode is required",
                "applicationCode"));

        RuleFor(x => x.RoleCode)
            .NotEmpty()
            .WithError(Error.Validation(
                "auth.role.empty",
                "RoleCode is required",
                "roleCode"));
    }
}

public class CheckPhoneExists : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("auth/phone/exists", async Task<EndpointResult> (
            CheckPhoneExistsHandler handler,
            string phone,
            string applicationCode,
            string roleCode,
            CancellationToken cancellationToken
        ) =>
        {
            return await handler.Handle(
                new CheckPhoneExistsRequest(phone, applicationCode, roleCode),
                cancellationToken);
        });
    }
}

public class CheckPhoneExistsHandler
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthReadDbConnection _readDb;
    private readonly IValidator<CheckPhoneExistsRequest> _validator;

    public CheckPhoneExistsHandler(
        UserManager<User> userManager,
        IAuthReadDbConnection readDb,
        IValidator<CheckPhoneExistsRequest> validator)
    {
        _userManager = userManager;
        _readDb = readDb;
        _validator = validator;
    }

    public async Task<UnitResult<Errors>> Handle(
        CheckPhoneExistsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.Phone, cancellationToken);

        if (user == null)
            return AuthErrors.UserNotFoundByPhone().ToErrors();

        var hasAccess = await _readDb.UserAccessesRead
            .AnyAsync(x => x.UserId == user.Id
                           && x.ApplicationCode == request.ApplicationCode
                           && x.RoleCode == request.RoleCode, cancellationToken);

        if (!hasAccess)
            return AuthErrors.NoAccessForRole().ToErrors();

        return UnitResult.Success<Errors>();
    }
}
