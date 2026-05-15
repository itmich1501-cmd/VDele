using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Services;

public sealed class AuthUserQueryService : IAuthUserQueryService
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthReadDbConnection _readDb;

    public AuthUserQueryService(
        UserManager<User> userManager,
        IAuthReadDbConnection readDb)
    {
        _userManager = userManager;
        _readDb = readDb;
    }

    public async Task<Result<UserInfo, Errors>> GetUserInfo(
        Guid userId,
        string applicationCode,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return AuthErrors.UserNotFound(userId).ToErrors();

        var roles = await _readDb.UserAccessesRead
            .Where(x => x.UserId == userId && x.ApplicationCode == applicationCode)
            .Select(x => x.RoleCode)
            .ToListAsync(cancellationToken);

        return new UserInfo(userId, user.PhoneNumber ?? string.Empty, roles);
    }
}
