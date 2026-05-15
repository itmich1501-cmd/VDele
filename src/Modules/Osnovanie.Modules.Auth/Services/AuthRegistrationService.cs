using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Abstractions.Persistence;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Services;

public sealed class AuthRegistrationService : IAuthRegistrationService
{
    private readonly UserManager<User> _userManager;
    private readonly IPhoneVerificationCodeRepository _phoneCodeRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly IAuthReadDbConnection _readDb;
    private readonly ILogger<AuthRegistrationService> _logger;

    public AuthRegistrationService(
        UserManager<User> userManager,
        IPhoneVerificationCodeRepository phoneCodeRepository,
        IUserAccessRepository userAccessRepository,
        IAuthReadDbConnection readDb,
        ILogger<AuthRegistrationService> logger)
    {
        _userManager = userManager;
        _phoneCodeRepository = phoneCodeRepository;
        _userAccessRepository = userAccessRepository;
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> RegisterByPhone(
        RegisterUserByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        var verificationCode = await _phoneCodeRepository.GetLatestActiveByPhone(
            command.Phone, cancellationToken);
        if (verificationCode is null)
            return AuthErrors.PhoneVerificationCode.NotFound().ToErrors();

        var confirmResult = verificationCode.Confirm(command.Code);
        if (confirmResult.IsFailure)
            return confirmResult.Error.ToErrors();

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == command.Phone, cancellationToken);

        User user;
        if (existingUser is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = command.Phone,
                NormalizedUserName = command.Phone.ToUpperInvariant(),
                PhoneNumber = command.Phone,
                PhoneNumberConfirmed = true,
                Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim()
            };

            var createUserResult = await _userManager.CreateAsync(
                user,
                string.IsNullOrWhiteSpace(command.Password) ? Guid.NewGuid().ToString().ToUpper() : command.Password);

            if (!createUserResult.Succeeded)
            {
                _logger.LogWarning(
                    "Failed to register user by phone {Phone}. Errors: {Errors}",
                    command.Phone,
                    string.Join(", ", createUserResult.Errors.Select(x => x.Description)));

                return AuthErrors.RegistrationFailed().ToErrors();
            }
        }
        else
        {
            user = existingUser;
        }

        var existingRoles = await _readDb.UserAccessesRead
            .Where(x => x.UserId == user.Id && x.ApplicationCode == command.ApplicationCode)
            .Select(x => x.RoleCode)
            .ToListAsync(cancellationToken);

        foreach (var roleCode in command.RoleCodes)
        {
            if (existingRoles.Contains(roleCode))
                continue;

            var userAccessResult = UserAccess.Create(
                user.Id,
                command.ApplicationCode,
                roleCode);

            if (userAccessResult.IsFailure)
                return userAccessResult.Error!.ToErrors();

            await _userAccessRepository.Add(userAccessResult.Value!, cancellationToken);
        }

        var markAsUsedResult = verificationCode.MarkAsUsed();
        if (markAsUsedResult.IsFailure)
            return markAsUsedResult.Error!.ToErrors();

        return user.Id;
    }

    public async Task<UnitResult<Errors>> AddRolesToUser(
        Guid userId,
        string applicationCode,
        IReadOnlyList<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return AuthErrors.UserNotFound(userId).ToErrors();

        var existingRoles = await _readDb.UserAccessesRead
            .Where(x => x.UserId == userId && x.ApplicationCode == applicationCode)
            .Select(x => x.RoleCode)
            .ToListAsync(cancellationToken);

        foreach (var roleCode in roleCodes)
        {
            if (existingRoles.Contains(roleCode))
                continue;

            var userAccessResult = UserAccess.Create(userId, applicationCode, roleCode);
            if (userAccessResult.IsFailure)
                return userAccessResult.Error!.ToErrors();

            await _userAccessRepository.Add(userAccessResult.Value!, cancellationToken);
        }

        return UnitResult.Success<Errors>();
    }
}
