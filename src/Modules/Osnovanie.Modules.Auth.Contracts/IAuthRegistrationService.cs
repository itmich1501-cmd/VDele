using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Contracts;

public interface IAuthRegistrationService
{
    Task<Result<Guid, Errors>> RegisterByPhone(
        RegisterUserByPhoneCommand command,
        CancellationToken cancellationToken);
}