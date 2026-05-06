using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts.Persistence;
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
    string FirstName);

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
    }
}

public class RegisterByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/phone/register", async (
            RegisterByPhoneHandler handler,
            [FromBody] RegisterByPhoneRequest request,
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
    private readonly IValidator<RegisterByPhoneRequest> _validator;
    private readonly IPhoneVerificationCodeRepository _phoneCodeRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RegisterByPhoneHandler> _logger;

    public RegisterByPhoneHandler(
        UserManager<User> userManager,
        IValidator<RegisterByPhoneRequest> validator,
        IPhoneVerificationCodeRepository phoneCodeRepository,
        IUserAccessRepository userAccessRepository,
        ITransactionManager transactionManager,
        ILogger<RegisterByPhoneHandler> logger)
    {
        _userManager = userManager;
        _validator = validator;
        _phoneCodeRepository = phoneCodeRepository;
        _userAccessRepository = userAccessRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Error.Validation(
                "auth.register.request.empty",
                "Тело запроса обязательно",
                "request").ToErrors();

        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == request.Phone, cancellationToken);

        if (existingUser is not null)
            return AuthErrors.UserAlreadyExists().ToErrors();

        var verificationCode = await _phoneCodeRepository.GetLatestConfirmedByPhone(
            request.Phone,
            cancellationToken);

        if (verificationCode is null)
            return AuthErrors.PhoneVerificationCode.NotConfirmed().ToErrors();

        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);

        if (transactionResult.IsFailure)
            return transactionResult.Error!.ToErrors();

        await using var transaction = transactionResult.Value!;

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Phone,
            NormalizedUserName = request.Phone.ToUpperInvariant(),
            FirstName = request.FirstName.Trim(),
            PhoneNumber = request.Phone,
            PhoneNumberConfirmed = true
        };

        var createUserResult = await _userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogWarning(
                "Failed to register user by phone {Phone}. Errors: {Errors}",
                request.Phone,
                string.Join(", ", createUserResult.Errors.Select(x => x.Description)));

            return AuthErrors.RegistrationFailed().ToErrors();
        }

        var userAccessResult = UserAccess.Create(
            user.Id,
            request.ApplicationCode,
            request.RoleCode);

        if (userAccessResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return userAccessResult.Error!.ToErrors();
        }

        await _userAccessRepository.Add(
            userAccessResult.Value!,
            cancellationToken);

        var markAsUsedResult = verificationCode.MarkAsUsed();

        if (markAsUsedResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return markAsUsedResult.Error!.ToErrors();
        }

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return saveResult.Error!.ToErrors();
        }

        var commitResult = await transaction.CommitAsync(cancellationToken);

        if (commitResult.IsFailure)
            return commitResult.Error!.ToErrors();

        return user.Id;
    }
}