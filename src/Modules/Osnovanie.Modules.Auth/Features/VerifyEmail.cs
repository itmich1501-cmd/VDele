using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.Extensions;

namespace Osnovanie.Modules.Auth.Features;

public record VerifyEmailRequest(Guid UserId, string Token);

public class VerifyEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("auth/email-verification", async (
            [FromQuery] Guid userId,
            [FromQuery] string token,
            VerifyEmailHandler handler,
            CancellationToken cancellationToken
        ) =>
        {
            var request = new VerifyEmailRequest(userId, token);
            
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class VerifyEmailHandler
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<VerifyEmailRequest> _validator;
    private readonly ILogger<VerifyEmailHandler> _logger;

    public VerifyEmailHandler(
        UserManager<User> userManager, 
        IValidator<VerifyEmailRequest> validator,
        ILogger<VerifyEmailHandler> logger)
    {
        _userManager = userManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email verification started. UserId: {UserId}",
            request.UserId);
        
        var validationResult = await _validator.ValidateAsync(request, cancellationToken)!;
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Email verification validation failed for UserId: {UserId}. Errors: {@Errors}",
                request.UserId,
                validationResult.Errors.Select(e => new { e.ErrorCode, e.ErrorMessage }));

            return validationResult.ToErrors();
        }
        
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            _logger.LogWarning(
                "Email verification failed. User not found. UserId: {UserId}",
                request.UserId);

            return AuthErrors.UserNotFound(request.UserId).ToErrors();
        }

        var result = await _userManager.ConfirmEmailAsync(
            user,
            Base64UrlEncoder.Decode(request.Token));

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Email verification failed for UserId: {UserId}. Errors: {@Errors}",
                request.UserId,
                result.Errors.Select(e => new { e.Code, e.Description }));
            
            var errors = result.Errors.Select(e =>
                Error.Validation(
                    $"auth.email_verification.{e.Code.ToLower()}",
                    e.Description));

            return errors.ToErrors();
        }
        
        _logger.LogInformation(
            "Email successfully verified. UserId: {UserId}",
            request.UserId);

        return user.Id;
    }
    
    public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
    {
        public VerifyEmailRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.Token)
                .NotEmpty()
                .Must(BeValidBase64Url)
                .WithMessage("Invalid token format");
        }

        private bool BeValidBase64Url(string token)
        {
            try
            {
                Base64UrlEncoder.Decode(token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}