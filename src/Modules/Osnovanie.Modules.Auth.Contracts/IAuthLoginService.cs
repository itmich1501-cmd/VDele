using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Contracts;

public interface IAuthLoginService
{
    Task<Result<string, Errors>> LoginByPhoneAnyRole(
        string phone,
        string code,
        string applicationCode,
        CancellationToken cancellationToken);

    Task<Result<string, Errors>> LoginByUsername(
        string username,
        string password,
        string applicationCode,
        string roleCode,
        CancellationToken ct);
}
