using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Customers.Domain;

namespace Osnovanie.Infrastructure.Repositories.VLavke;

public sealed class VLavkeCustomerProfileRepository : IVLavkeCustomerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public VLavkeCustomerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(
        VLavkeCustomerProfile profile,
        CancellationToken cancellationToken)
    {
        await _dbContext.VLavkeCustomerProfiles.AddAsync(profile, cancellationToken);
    }
}