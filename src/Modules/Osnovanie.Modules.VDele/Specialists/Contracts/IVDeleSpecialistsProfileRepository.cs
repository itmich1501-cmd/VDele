using Osnovanie.Modules.VDele.Specialists.Domain;

namespace Osnovanie.Modules.VDele.Specialists.Contracts;

public interface IVDeleSpecialistProfileRepository
{
    Task Add(
        VDeleSpecialistProfile vDeleSpecialistProfile,
        CancellationToken cancellationToken);
}