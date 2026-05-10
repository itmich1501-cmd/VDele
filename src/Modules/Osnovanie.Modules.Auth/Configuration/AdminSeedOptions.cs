namespace Osnovanie.Modules.Auth.Configuration;

public class AdminSeedOptions
{
    public const string SECTION_NAME = "AdminSeed";

    public List<AdminSeed> Admins { get; set; } = new();
}

public class AdminSeed
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApplicationCode { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}