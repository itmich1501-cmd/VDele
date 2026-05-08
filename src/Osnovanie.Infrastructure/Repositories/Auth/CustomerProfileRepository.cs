using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public sealed class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByUserId(Guid userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task Add(
        CustomerProfile profile,
        CancellationToken cancellationToken)
    {
        await _dbContext.VDeleCustomerProfiles.AddAsync(profile, cancellationToken);
    }
}