using Osnovanie.Modules.VDele.Specialists.Domain;

namespace Osnovanie.Modules.VDele.Specialists.Contracts;

public interface IVDeleSpecialistsReadDbContext
{
    IQueryable<VDeleSpecialistProfile> SpecialistProfilesRead { get; }
}