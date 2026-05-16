using Osnovanie.Modules.VLavke.Sellers.Domain;

namespace Osnovanie.Modules.VLavke.Sellers.Contracts;

public interface IVLavkeSellerProfileRepository
{
    Task Add(
        VLavkeSellerProfile vLavkeSellerProfile,
        CancellationToken cancellationToken);

    Task<VLavkeSellerProfile?> GetByUserId(
        Guid userId,
        CancellationToken cancellationToken);
}