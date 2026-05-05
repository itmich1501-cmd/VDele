using Osnovanie.Modules.ReferenceData.Cities.Domain;

namespace Osnovanie.Modules.ReferenceData.DataBase;

public interface IReadDbConnection
{
    IQueryable<City> CitiesRead { get; }
}