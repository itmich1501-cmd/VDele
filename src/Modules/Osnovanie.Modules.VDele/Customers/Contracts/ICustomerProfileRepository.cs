using Osnovanie.Modules.VDele.Customers.Domain;

namespace Osnovanie.Modules.VDele.Customers.Contracts;

public interface ICustomerProfileRepository
{
    Task<bool> ExistsByUserId(
        Guid userId,
        CancellationToken cancellationToken);

    Task Add(
        CustomerProfile customerProfile,
        CancellationToken cancellationToken);
}