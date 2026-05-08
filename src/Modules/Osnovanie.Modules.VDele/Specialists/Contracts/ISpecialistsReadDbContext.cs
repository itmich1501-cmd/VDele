using Osnovanie.Modules.VDele.Specialists.Domain;

namespace Osnovanie.Modules.VDele.Specialists.Contracts;

public interface ISpecialistsReadDbContext
{
    IQueryable<SpecialistProfile> SpecialistProfilesRead { get; }
}