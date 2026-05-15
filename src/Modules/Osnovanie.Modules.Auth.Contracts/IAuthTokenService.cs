using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Contracts;

public interface IAuthTokenService
{
    Task<Result<string, Errors>> GenerateTokenForUser(Guid userId, string applicationCode, CancellationToken ct);
}
