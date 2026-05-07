using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Contracts.Persistence;

public interface IVLavkeSellerProfileRepository
{
    Task Add(VLavkeSellerProfile profile, CancellationToken cancellationToken);
}