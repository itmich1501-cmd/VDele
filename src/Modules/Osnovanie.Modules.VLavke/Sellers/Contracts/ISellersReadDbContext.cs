using Osnovanie.Modules.VLavke.Sellers.Domain;

namespace Osnovanie.Modules.VLavke.Sellers.Contracts;

public interface ISellersReadDbContext
{
    IQueryable<SellerProfile> SellerProfilesRead { get; }
}