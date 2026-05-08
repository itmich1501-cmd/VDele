using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Abstractions.Persistence;

public interface IUserAccessRepository
{
    Task Add(UserAccess userAccess, CancellationToken cancellationToken);
}