using Osnovanie.Modules.VDele.Specialists.Domain;

namespace Osnovanie.Modules.VDele.Specialists.Contracts;

public interface ISpecialistProfileRepository
{
    Task<bool> ExistsByUserId(
        Guid userId,
        CancellationToken cancellationToken);

    Task Add(
        SpecialistProfile specialistProfile,
        CancellationToken cancellationToken);
}