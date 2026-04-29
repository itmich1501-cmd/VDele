namespace Osnovanie.Modules.Auth.Jwt;

public class JwtOptions
{
    public const string SECTION_NAME = "JwtOptions";
    
    public string SecretKey { get; init; } = string.Empty;
    
    public int TokenLifeTimeInMinutes { get; init; }
    
    public string Issuer { get; init; } = string.Empty; 
}