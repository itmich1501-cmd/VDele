using Osnovanie.Modules.VLavke.Sellers.Domain;

namespace Osnovanie.Modules.VLavke.Sellers.Contracts;

public interface ISellerProfileRepository
{
    Task<bool> ExistsByUserId(
        Guid userId,
        CancellationToken cancellationToken);

    Task Add(
        SellerProfile sellerProfile,
        CancellationToken cancellationToken);
}