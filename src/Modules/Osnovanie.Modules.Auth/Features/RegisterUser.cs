using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;

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

public class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/registration", async (
            RegisterUserHandler handler,
            RegisterUserRequest request,
            CancellationToken cancellationToken
            ) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterUserHandler
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<RegisterUserRequest> _validator;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<User> userManager, 
        IValidator<RegisterUserRequest> validator,
        ILogger<RegisterUserHandler> logger) 
    {
        _userManager = userManager;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<Guid, Errors>> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new user account for email {Email}", request.Email);
        
        var validationResult = await _validator.ValidateAsync(request, cancellationToken)!;
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Register user validation failed for email {Email}. Errors: {@Errors}",
                request.Email,
                validationResult.Errors.Select(e => new { e.ErrorCode, e.ErrorMessage }));
            
            return new Errors(validationResult.Errors.Select(e => Error.Validation(e.ErrorCode, e.ErrorMessage)));
        }
        
        var user = new User
        {
            Email = request.Email,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "User creation failed for email {Email}. Errors: {@Errors}",
                request.Email,
                result.Errors.Select(e => new { e.Code, e.Description }));
            
            var errors = result.Errors.Select(e => Error.Failure(e.Code, e.Description));
            return new Errors(errors);
        }
        
        _logger.LogInformation("User account created successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
        
        return user.Id;
    }
}

