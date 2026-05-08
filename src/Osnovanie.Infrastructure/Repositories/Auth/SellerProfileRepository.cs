using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public sealed class SellerProfileRepository : ISellerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public SellerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(
        VLavkeSellerProfile profile,
        CancellationToken cancellationToken)
    {
        await _dbContext.VLavkeSellerProfiles.AddAsync(profile, cancellationToken);
    }
}