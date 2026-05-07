using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public class VDeleSpecialistProfileRepository : IVDeleSpecialistProfileRepository
{
    private readonly AppDbContext _dbContext;

    public VDeleSpecialistProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task Add(VDeleSpecialistProfile profile, CancellationToken cancellationToken)
    {
        await _dbContext.VDeleSpecialistProfiles.AddAsync(profile, cancellationToken);
    }
}