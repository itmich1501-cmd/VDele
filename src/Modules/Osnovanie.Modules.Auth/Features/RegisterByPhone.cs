using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Email;
using Osnovanie.Shared.Extensions;

namespace Osnovanie.Modules.Auth.Features;

public record RegisterByPhoneRequest(
    string Phone,
    string Password,
    string FirstName,
    string ApplicationCode,
    string RoleCode);

public class RegisterByPhoneRequestValidator : AbstractValidator<RegisterByPhoneRequest>
{
    public RegisterByPhoneRequestValidator()
    {
        RuleFor(x => x.Phone)
            .Phone();

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(Error.Validation("auth.password.empty", "Пароль обязателен", "password"))
            .MinimumLength(6)
            .WithError(Error.Validation("auth.password.too_short", "Минимум 6 символов", "password"))
            .MaximumLength(100)
            .WithError(Error.Validation("auth.password.too_long", "Максимум 100 символов", "password"));

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithError(Error.Validation("auth.firstname.empty", "Имя обязательно", "firstName"))
            .MaximumLength(50)
            .WithError(Error.Validation("auth.firstname.too_long", "Максимум 50 символов", "firstName"));

        RuleFor(x => x.ApplicationCode)
            .NotEmpty()
            .WithError(Error.Validation("auth.application.empty", "Код приложения обязателен", "applicationCode"));

        RuleFor(x => x.RoleCode)
            .NotEmpty()
            .WithError(Error.Validation("auth.role.empty", "Роль обязательна", "roleCode"));
    }
}

public class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/phone/register", async (
            RegisterByPhoneHandler handler,
            RegisterByPhoneRequest request,
            CancellationToken cancellationToken
            ) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterByPhoneHandler
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<RegisterByPhoneRequest> _validator;
    private readonly ILogger<RegisterByPhoneHandler> _logger;
    private readonly ITransactionManager _transactionManager;

    public RegisterByPhoneHandler(
        UserManager<User> userManager,
        IEmailSender  emailSender,
        IValidator<RegisterByPhoneRequest> validator,
        ILogger<RegisterByPhoneHandler> logger, 
        ITransactionManager transactionManager) 
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _validator = validator;
        _logger = logger;
        _transactionManager = transactionManager;
    }
    
    public async Task<Result<Guid, Error>> Handle(
        RegisterByPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == request.Phone, cancellationToken);

        if (existingUser is not null)
            return AuthErrors.UserAlreadyExists();

        var verificationCodeResult = await _codeRepository.GetActiveCode(
            request.Phone,
            request.Code,
            cancellationToken);

        if (verificationCodeResult.IsFailure)
            return verificationCodeResult.Error;

        var verificationCode = verificationCodeResult.Value;

        if (verificationCode.IsExpired())
            return AuthErrors.PhoneVerificationCode.Expired();

        verificationCode.MarkAsUsed();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Phone,
            PhoneNumber = request.Phone,
            PhoneNumberConfirmed = true,
            NormalizedUserName = request.FirstName
        };

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionScopeResult.IsFailure)
        {
            return transactionScopeResult.Error;
        }

        await using var transactionScope = transactionScopeResult.Value;

        var createUserResult = await _userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Errors.Auth.InvalidCredentials();
        }

        var userAccess = UserAccess.Create(
            user.Id,
            ApplicationCodes.Vdele,
            RoleCodes.Customer);

        if (userAccess.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);

            return userAccess.Error;
        }

        await _userAccessRepository.Add(userAccess.Value, cancellationToken);

        await _transactionManager.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return user.Id;
    }
}