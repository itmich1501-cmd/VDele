using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Contracts.Persistence;

public interface IVDeleSpecialistProfileRepository
{
    Task Add(VDeleSpecialistProfile profile, CancellationToken cancellationToken);
}