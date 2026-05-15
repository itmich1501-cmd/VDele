using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Contracts;

public interface IAuthRegistrationService
{
    Task<Result<Guid, Errors>> RegisterByPhone(
        RegisterUserByPhoneCommand command,
        CancellationToken cancellationToken);

    Task<UnitResult<Errors>> AddRolesToUser(
        Guid userId,
        string applicationCode,
        IReadOnlyList<string> roleCodes,
        CancellationToken cancellationToken);
}