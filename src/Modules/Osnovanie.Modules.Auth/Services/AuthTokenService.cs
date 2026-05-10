using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Services;

public class AuthTokenService : IAuthTokenService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IAuthReadDbConnection _authReadDbConnection;

    public AuthTokenService(UserManager<User> userManager,
        ITokenGenerator tokenGenerator,
        IAuthReadDbConnection authReadDbConnection)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _authReadDbConnection = authReadDbConnection;
    }
    
    public async Task<Result<string, Errors>> GenerateTokenForUser(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return AuthErrors.InvalidCredentials().ToErrors();
        }
        
        var role = await _authReadDbConnection.UserAccessesRead.FirstOrDefaultAsync(u => u.UserId == user.Id, ct);
        if (role == null)
        {
            return AuthErrors.InvalidCredentials().ToErrors();
        }
        
        return _tokenGenerator.GenerateToken(user, role.RoleCode, role.ApplicationCode);
    }
}