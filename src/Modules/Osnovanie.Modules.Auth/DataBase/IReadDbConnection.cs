using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.DataBase;

public interface IReadDbConnection
{
    IQueryable<UserAccess> UserAccessesRead { get; }
}