using Osnovanie.Modules.ReferenceData.Cities.Domain;

namespace Osnovanie.Modules.ReferenceData.Contracts;

public interface IReferenceDataReadDbContext
{
    IQueryable<City> CitiesRead { get; }
}