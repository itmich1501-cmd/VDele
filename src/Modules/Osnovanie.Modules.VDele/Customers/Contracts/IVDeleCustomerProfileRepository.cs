using Osnovanie.Modules.VDele.Customers.Domain;

namespace Osnovanie.Modules.VDele.Customers.Contracts;

public interface IVDeleCustomerProfileRepository
{
    Task<bool> ExistsByUserId(
        Guid userId,
        CancellationToken cancellationToken);

    Task Add(
        VDeleCustomerProfile vDeleCustomerProfile,
        CancellationToken cancellationToken);
    
    Task<VDeleCustomerProfile?> GetByUserId(
        Guid userId, 
        CancellationToken cancellationToken);
}