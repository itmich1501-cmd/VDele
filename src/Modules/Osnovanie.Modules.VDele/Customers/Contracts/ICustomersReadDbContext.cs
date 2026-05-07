using Osnovanie.Modules.VDele.Customers.Domain;

namespace Osnovanie.Modules.VDele.Customers.Contracts;

public interface ICustomersReadDbContext
{
    IQueryable<CustomerProfile> CustomerProfilesRead { get; }
}