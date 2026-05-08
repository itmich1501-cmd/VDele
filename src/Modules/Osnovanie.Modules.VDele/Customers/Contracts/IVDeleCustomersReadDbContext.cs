using Osnovanie.Modules.VDele.Customers.Domain;

namespace Osnovanie.Modules.VDele.Customers.Contracts;

public interface IVDeleCustomersReadDbContext
{
    IQueryable<VDeleCustomerProfile> CustomerProfilesRead { get; }
}