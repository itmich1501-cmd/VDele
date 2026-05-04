using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Infrastructure.Configuration;

public static class IdentityConfiguration
{
    public static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users");

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");

        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");

        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");

        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
    }
}