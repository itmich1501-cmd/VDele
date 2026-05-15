using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Auth.Features;

public sealed record VDeleLoginByPhoneRequest(string Phone, string Code);

public sealed class VDeleLoginByPhoneValidator : AbstractValidator<VDeleLoginByPhoneRequest>
{
    public VDeleLoginByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(Error.Validation(
                "vdele.auth.phone.empty",
                "Phone is required",
                "phone"));

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithError(Error.Validation(
                "vdele.auth.code.invalid",
                "Code must be 4 digits",
                "code"));
    }
}

public sealed class VDeleLoginByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vdele/auth/login-by-phone", async (
            VDeleLoginByPhoneHandler handler,
            VDeleLoginByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);
            return new EndpointResult<string>(result);
        });
    }
}

public sealed class VDeleLoginByPhoneHandler
{
    private readonly IAuthLoginService _authLoginService;
    private readonly IValidator<VDeleLoginByPhoneRequest> _validator;

    public VDeleLoginByPhoneHandler(
        IAuthLoginService authLoginService,
        IValidator<VDeleLoginByPhoneRequest> validator)
    {
        _authLoginService = authLoginService;
        _validator = validator;
    }

    public async Task<Result<string, Errors>> Handle(
        VDeleLoginByPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        return await _authLoginService.LoginByPhoneAnyRole(
            request.Phone,
            request.Code,
            ApplicationCodes.VDele,
            cancellationToken);
    }
}
