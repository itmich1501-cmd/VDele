using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

public sealed class PhoneRegistrationService
{
    private readonly UserManager<User> _userManager;
    private readonly IPhoneVerificationCodeRepository _phoneCodeRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<PhoneRegistrationService> _logger;

    public PhoneRegistrationService(
        UserManager<User> userManager,
        IPhoneVerificationCodeRepository phoneCodeRepository,
        IUserAccessRepository userAccessRepository,
        ITransactionManager transactionManager,
        ILogger<PhoneRegistrationService> logger)
    {
        _userManager = userManager;
        _phoneCodeRepository = phoneCodeRepository;
        _userAccessRepository = userAccessRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Register(
        PhoneRegistrationCommand command,
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

        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);

        if (transactionResult.IsFailure)
            return transactionResult.Error!.ToErrors();

        await using var transaction = transactionResult.Value!;

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = command.Phone,
            NormalizedUserName = command.Phone.ToUpperInvariant(),
            PhoneNumber = command.Phone,
            PhoneNumberConfirmed = true,
            FirstName = command.FirstName.Trim(),
        };

        var createUserResult = await _userManager.CreateAsync(user, command.Password);

        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

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