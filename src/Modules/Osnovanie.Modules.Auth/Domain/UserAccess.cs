using CSharpFunctionalExtensions;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Domain;

public record UserAccessId(Guid Id);

public sealed class UserAccess
{
    public UserAccessId Id { get; private set; }

    public Guid UserId { get; private set; }

    public string ApplicationCode { get; private set; } // VDele, VLavke, AdminPanel

    public string RoleCode { get; private set; } // Customer, Specialist, Admin...

    public DateTime CreatedAt { get; private set; }
    
    private UserAccess()
    {
        // For EF Core
    }

    private UserAccess(
        Guid id,
        Guid userId,
        string applicationCode,
        string roleCode,
        DateTime createdAt)
        
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }
        
        Id = new UserAccessId(id);
        UserId = userId;
        ApplicationCode = applicationCode;
        RoleCode = roleCode;
        CreatedAt = createdAt;
    }

    public static Result<UserAccess, Error> Create(
        Guid userId,
        string applicationCode,
        string roleCode)
    {
        if (userId == Guid.Empty)
            return AuthErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(applicationCode))
            return AuthErrors.ApplicationCodeIsEmpty();

        if (string.IsNullOrWhiteSpace(roleCode))
            return AuthErrors.RoleCodeIsEmpty();

        var userAccess = new UserAccess(
            id: Guid.NewGuid(),
            userId: userId,
            applicationCode: applicationCode.Trim(),
            roleCode: roleCode.Trim(),
            createdAt: DateTime.UtcNow);

        return userAccess;
    }
}