using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public sealed class UserAccessRepository : IUserAccessRepository
{
    private readonly AppDbContext _dbContext;

    public UserAccessRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(
        UserAccess userAccess,
        CancellationToken cancellationToken)
    {
        await _dbContext.UserAccesses.AddAsync(userAccess, cancellationToken);
    }
}