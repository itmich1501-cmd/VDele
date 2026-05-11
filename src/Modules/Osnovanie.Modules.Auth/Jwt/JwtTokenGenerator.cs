using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Jwt;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }
    //получение ролей тоже здесь будет
    public Result<string, Errors> GenerateToken(User user, string roleCode, string  applicationCode)
    {
        Claim[] claims = 
        [
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Email, user.Email ?? ""),
            new (ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.Role, roleCode),
            new("application", applicationCode)
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
        var jwtToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.TokenLifetimeMinutes),
            signingCredentials: signingCredentials);

        var jwtStringToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return jwtStringToken;
    }
}