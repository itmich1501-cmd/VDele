using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Features;

public record LoginRequest(string Email, string Password);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithError(Error.Validation("email.is.invalid", "Неверный формат Email", "email"))
            .EmailAddress().WithError(Error.Validation("email.is.invalid", "Неверный формат Email", "email"));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Error.Validation("password.is.invalid", "Неверный формат пароля", "password"))
            .MinimumLength(6)
            .WithError(Error.Validation("password.is.invalid", "Неверный формат пароля", "password"))
            .MaximumLength(100)
            .WithError(Error.Validation("password.is.invalid", "Неверный формат пароля", "password"));
    }
}

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/login", async (
            LoginRequest request,
            LoginHandler handler,
            CancellationToken cancellationToken
        ) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<string>(result);
        });
    }
}

public class LoginHandler
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginHandler(UserManager<User> userManager, ITokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<string, Errors>> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return AuthErrors.InvalidCredentials().ToErrors();
        }

        var checkPasswordResult = await _userManager.CheckPasswordAsync(user, request.Password);
        if (checkPasswordResult == false)
        {
            return AuthErrors.InvalidCredentials().ToErrors();
        }
        
        var jwtResult = _tokenGenerator.GenerateToken(user);
        if (jwtResult.IsFailure)
        {
            return jwtResult.Error;
        }

        return jwtResult.Value;
    }
}