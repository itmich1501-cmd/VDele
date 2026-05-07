using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public sealed class VDeleCustomerProfileRepository : IVDeleCustomerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public VDeleCustomerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(
        VDeleCustomerProfile profile,
        CancellationToken cancellationToken)
    {
        await _dbContext.VDeleCustomerProfiles.AddAsync(profile, cancellationToken);
    }
}