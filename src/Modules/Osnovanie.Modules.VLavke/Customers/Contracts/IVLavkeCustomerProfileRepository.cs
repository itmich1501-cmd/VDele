using Osnovanie.Modules.VLavke.Customers.Domain;

namespace Osnovanie.Modules.VLavke.Customers.Contracts;

public interface IVLavkeCustomerProfileRepository
{
    Task<bool> ExistsByUserId(
        Guid userId,
        CancellationToken cancellationToken);

    Task Add(
        IVLavkeCustomerProfile ivLavkeCustomerProfile,
        CancellationToken cancellationToken);
}