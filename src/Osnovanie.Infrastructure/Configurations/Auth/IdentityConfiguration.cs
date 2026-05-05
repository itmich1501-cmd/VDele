using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Configurations.Auth;

public static class IdentityConfiguration
{
    public static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users", "auth");

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles", "auth");

        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", "auth");

        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", "auth");

        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", "auth");

        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", "auth");

        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles", "auth");
    }
}