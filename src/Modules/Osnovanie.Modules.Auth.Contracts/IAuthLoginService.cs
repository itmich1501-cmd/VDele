using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Contracts;

public interface IAuthLoginService
{
    Task<Result<string, Errors>> LoginByPhone(
        string phone,
        string code,
        string applicationCode,
        string roleCode,
        CancellationToken cancellationToken);
    
    Task<Result<string, Errors>> LoginByUsername(
        string username,
        string password,
        string applicationCode,
        string roleCode,
        CancellationToken ct);
}