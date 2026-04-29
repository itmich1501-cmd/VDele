using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared.Email;
using Osnovanie.Shared.Extensions;

namespace Osnovanie.Modules.Auth.Features;

public record RegisterUserRequest(string Email, string Password);

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithError(Error.Validation("auth.email.empty", "Email обязателен", "email"))
            .EmailAddress().WithError(Error.Validation("auth.email.invalid", "Неверный формат Email", "email"));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Error.Validation("auth.password.empty", "Пароль обязателен", "password"))
            .MinimumLength(6)
            .WithError(Error.Validation("auth.password.too_short", "Минимум 6 символов", "password"))
            .MaximumLength(100)
            .WithError(Error.Validation("auth.password.too_long", "Максимум 100 символов", "password"));
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
    private readonly IEmailSender _emailSender;
    private readonly IValidator<RegisterUserRequest> _validator;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<User> userManager,
        IEmailSender  emailSender,
        IValidator<RegisterUserRequest> validator,
        ILogger<RegisterUserHandler> logger) 
    {
        _userManager = userManager;
        _emailSender = emailSender;
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

            return validationResult.ToErrors();
        }
        
        var user = new User
        {
            Email = request.Email,
            UserName = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "User creation failed for email {Email}. Errors: {@Errors}",
                request.Email,
                result.Errors.Select(e => new { e.Code, e.Description }));
            
            var errors = result.Errors.Select(e =>
                Error.Validation(
                    $"auth.identity.{e.Code.ToLower()}",
                    e.Description
                ));

            return errors.ToErrors();
        }
        
        _logger.LogInformation("User account created successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
        
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink =
            "http://localhost:5287/api/auth/email-verification/" +
            "?userId=" + user.Id +
            "&token=" + Base64UrlEncoder.Encode(confirmationToken);
            
        _logger.LogInformation(
            "Sending confirmation email to {Email}",
            request.Email);
        
        var emailResult = await _emailSender.SendAsync(new MailData(
            request.Email,
            "Подтверждение регистрации",
            $"Для подтверждения регистрации перейдите по ссылке: {confirmationLink}"));
        
        if (emailResult.IsFailure)
        {
            _logger.LogError(
                "User created but email sending failed. UserId: {UserId}, Email: {Email}. Errors: {@Errors}",
                user.Id,
                request.Email,
                emailResult.Error);

            return AuthErrors.EmailSendFailed().ToErrors();
        }

        _logger.LogInformation(
            "Confirmation email successfully sent to {Email}",
            request.Email);
        
        return user.Id;
    }
}

