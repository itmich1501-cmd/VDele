using Osnovanie.Modules.VLavke.Customers.Domain;

namespace Osnovanie.Modules.VLavke.Customers.Contracts;

public interface IVLavkeCustomersReadDbContext
{
    IQueryable<VLavkeCustomerProfile> CustomerProfilesRead { get; }
}