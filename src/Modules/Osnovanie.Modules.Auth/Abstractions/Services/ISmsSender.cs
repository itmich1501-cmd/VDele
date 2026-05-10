using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Abstractions.Services;

public interface ISmsSender
{
    Task<UnitResult<Error>> SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}
