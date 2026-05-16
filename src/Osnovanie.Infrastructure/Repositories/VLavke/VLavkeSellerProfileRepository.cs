using Microsoft.EntityFrameworkCore;
using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.Domain;

namespace Osnovanie.Infrastructure.Repositories.VLavke;

public sealed class VLavkeSellerProfileRepository : IVLavkeSellerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public VLavkeSellerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(
        VLavkeSellerProfile profile,
        CancellationToken cancellationToken)
    {
        await _dbContext.VLavkeSellerProfiles.AddAsync(profile, cancellationToken);
    }

    public Task<VLavkeSellerProfile?> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.VLavkeSellerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}