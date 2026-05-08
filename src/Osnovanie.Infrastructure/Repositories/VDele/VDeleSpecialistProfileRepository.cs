using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VDele.Specialists.Domain;

namespace Osnovanie.Infrastructure.Repositories.VDele;

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