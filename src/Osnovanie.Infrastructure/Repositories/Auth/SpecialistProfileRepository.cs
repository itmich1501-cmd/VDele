using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.VDele.Specialists.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public class SpecialistProfileRepository : ISpecialistProfileRepository
{
    private readonly AppDbContext _dbContext;

    public SpecialistProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task Add(SpecialistProfile profile, CancellationToken cancellationToken)
    {
        await _dbContext.VDeleSpecialistProfiles.AddAsync(profile, cancellationToken);
    }
}