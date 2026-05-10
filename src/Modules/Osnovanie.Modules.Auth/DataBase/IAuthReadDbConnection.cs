using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.DataBase;

public interface IAuthReadDbConnection
{
    IQueryable<UserAccess> UserAccessesRead { get; }
}