using Osnovanie.Modules.VLavke.Customers.Domain;

namespace Osnovanie.Modules.VLavke.Customers.Contracts;

public interface IVLavkeCustomerProfileRepository
{
    Task Add(
        VLavkeCustomerProfile vLavkeCustomerProfile,
        CancellationToken cancellationToken);
}