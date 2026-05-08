using Osnovanie.Modules.VLavke.Sellers.Domain;

namespace Osnovanie.Modules.VLavke.Sellers.Contracts;

public interface IVLavkeSellersReadDbContext
{
    IQueryable<VLavkeSellerProfile> SellerProfilesRead { get; }
}