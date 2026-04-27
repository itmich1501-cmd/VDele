using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared;
using Osnovanie.Framework.EndpointResult;

namespace Osnovanie.Modules.Auth.Features;

public record RegisterUserRequest(string Email, string Password);

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().
            EmailAddress();
        RuleFor(r => r.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(100);
    }
}

[ApiController]
[Route("api/auth")]
public sealed class RegisterUserController : ControllerBase
{
    private readonly RegisterUserHandler _handler;

    public RegisterUserController(RegisterUserHandler handler)
    {
        _handler = handler;
    }
    
    [Route("/registration")]
    [HttpPost]
    public async Task<EndpointResult<Guid>> RegisterUser([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        return await _handler.Handle(request, cancellationToken);
    }
}

public sealed class RegisterUserHandler
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<RegisterUserRequest> _validator;

    public RegisterUserHandler(UserManager<User> userManager, IValidator<RegisterUserRequest> validator)
    {
        _userManager = userManager;
        _validator = validator;
    }
    
    public async Task<Result<Guid, Errors>> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult.Errors.Select(e => Error.Validation(e.ErrorCode, e.ErrorMessage)));
        }
        
        var user = new User
        {
            Email = request.Email,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => Error.Failure(e.Code, e.Description));
            return new Errors(errors);
        }
        
        return user.Id;
    }
}