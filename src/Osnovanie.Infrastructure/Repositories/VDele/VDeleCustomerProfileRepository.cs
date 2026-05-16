using Microsoft.EntityFrameworkCore;
using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.Domain;

namespace Osnovanie.Infrastructure.Repositories.VDele;

public sealed class VDeleCustomerProfileRepository : IVDeleCustomerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public VDeleCustomerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByUserId(Guid userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task Add(
        VDeleCustomerProfile profile,
        CancellationToken cancellationToken)
    {
        await _dbContext.VDeleCustomerProfiles.AddAsync(profile, cancellationToken);
    }

    public async Task<VDeleCustomerProfile?> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.VDeleCustomerProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken: cancellationToken);
    }
}