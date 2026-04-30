using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Domain;

public sealed class UserAccess
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string ApplicationCode { get; private set; } = null!; // VDele, VLavke, AdminPanel

    public string RoleCode { get; private set; } = null!; // Customer, Specialist, Admin...

    public DateTime CreatedAt { get; private set; }

    public static Result<UserAccess, Error> Create(
        Guid userId,
        string applicationCode,
        string roleCode
        )
    {
        if (us)
    }
}