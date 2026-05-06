using Microsoft.AspNetCore.Identity;

namespace Osnovanie.Modules.Auth.Domain;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = null!;
}