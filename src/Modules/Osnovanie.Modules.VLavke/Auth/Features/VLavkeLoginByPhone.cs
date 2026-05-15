using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Auth.Features;

public sealed record VLavkeLoginByPhoneRequest(string Phone, string Code);

public sealed class VLavkeLoginByPhoneValidator : AbstractValidator<VLavkeLoginByPhoneRequest>
{
    public VLavkeLoginByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithError(Error.Validation(
                "vlavke.auth.phone.empty",
                "Phone is required",
                "phone"));

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithError(Error.Validation(
                "vlavke.auth.code.invalid",
                "Code must be 4 digits",
                "code"));
    }
}

public sealed class VLavkeLoginByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("vlavke/auth/login-by-phone", async (
            VLavkeLoginByPhoneHandler handler,
            VLavkeLoginByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);
            return new EndpointResult<string>(result);
        });
    }
}

public sealed class VLavkeLoginByPhoneHandler
{
    private readonly IAuthLoginService _authLoginService;
    private readonly IValidator<VLavkeLoginByPhoneRequest> _validator;

    public VLavkeLoginByPhoneHandler(
        IAuthLoginService authLoginService,
        IValidator<VLavkeLoginByPhoneRequest> validator)
    {
        _authLoginService = authLoginService;
        _validator = validator;
    }

    public async Task<Result<string, Errors>> Handle(
        VLavkeLoginByPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        return await _authLoginService.LoginByPhoneAnyRole(
            request.Phone,
            request.Code,
            ApplicationCodes.VLavke,
            cancellationToken);
    }
}
