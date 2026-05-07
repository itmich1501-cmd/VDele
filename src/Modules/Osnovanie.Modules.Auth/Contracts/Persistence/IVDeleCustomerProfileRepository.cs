using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Contracts.Persistence;

public interface IVDeleCustomerProfileRepository
{
    Task Add(VDeleCustomerProfile profile, CancellationToken cancellationToken);
}