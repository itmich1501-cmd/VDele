using Osnovanie.Modules.ReferenceData.Cities.Domain;

namespace Osnovanie.Modules.ReferenceData.DataBase;

public interface IReferenceDataReadDbContext
{
    IQueryable<City> CitiesRead { get; }
}