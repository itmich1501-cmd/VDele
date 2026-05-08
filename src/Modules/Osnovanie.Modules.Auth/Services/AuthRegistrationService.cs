using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Services;

public sealed record PhoneRegistrationCommand(
    string Phone,
    string Password,
    string FirstName,
    string ApplicationCode,
    string RoleCode,
    Guid CityId);

public sealed class AuthRegistrationService : IAuthRegistrationService
{
    private readonly UserManager<User> _userManager;
    private readonly IPhoneVerificationCodeRepository _phoneCodeRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly ILogger<AuthRegistrationService> _logger;

    public AuthRegistrationService(
        UserManager<User> userManager,
        IPhoneVerificationCodeRepository phoneCodeRepository,
        IUserAccessRepository userAccessRepository,
        ILogger<AuthRegistrationService> logger)
    {
        _userManager = userManager;
        _phoneCodeRepository = phoneCodeRepository;
        _userAccessRepository = userAccessRepository;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> RegisterByPhone(
        RegisterUserByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.Phone, cancellationToken);

        if (existingUser is not null)
            return AuthErrors.UserAlreadyExists().ToErrors();

        var verificationCode = await _phoneCodeRepository.GetLatestConfirmedByPhone(
            command.Phone,
            cancellationToken);

        if (verificationCode is null)
            return AuthErrors.PhoneVerificationCode.NotConfirmed().ToErrors();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = command.Phone,
            NormalizedUserName = command.Phone.ToUpperInvariant(),
            PhoneNumber = command.Phone,
            PhoneNumberConfirmed = true,
            Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim()
        };

        var createUserResult = await _userManager.CreateAsync(user, command.Password);

        if (!createUserResult.Succeeded)
        {
            _logger.LogWarning(
                "Failed to register user by phone {Phone}. Errors: {Errors}",
                command.Phone,
                string.Join(", ", createUserResult.Errors.Select(x => x.Description)));

            return AuthErrors.RegistrationFailed().ToErrors();
        }

        var userAccessResult = UserAccess.Create(
            user.Id,
            command.ApplicationCode,
            command.RoleCode);

        if (userAccessResult.IsFailure)
            return userAccessResult.Error!.ToErrors();

        await _userAccessRepository.Add(
            userAccessResult.Value!,
            cancellationToken);

        var markAsUsedResult = verificationCode.MarkAsUsed();

        if (markAsUsedResult.IsFailure)
            return markAsUsedResult.Error!.ToErrors();

        return user.Id;
    }
}