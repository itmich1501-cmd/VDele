using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Contracts;

public interface IAuthUserQueryService
{
    Task<Result<UserInfo, Errors>> GetUserInfo(
        Guid userId,
        string applicationCode,
        CancellationToken cancellationToken);
}
