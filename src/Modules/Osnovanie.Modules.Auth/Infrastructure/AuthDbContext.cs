using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Infrastructure.Configuration;

namespace Osnovanie.Modules.Auth.Infrastructure;

public class AuthDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public readonly string _connectionString;
    
    public AuthDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       optionsBuilder.UseNpgsql(_connectionString);
       optionsBuilder.EnableSensitiveDataLogging();
       optionsBuilder.UseLoggerFactory(CreateLoggerFactory());
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("auth");
        
        IdentityConfiguration.ConfigureIdentity(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }

    private ILoggerFactory CreateLoggerFactory() =>
        LoggerFactory.Create(builder => { builder.AddConsole(); });
    
    public IQueryable<UserAccess> UserAccessesRead => Set<UserAccess>().AsQueryable().AsNoTracking();
    public IQueryable<PhoneVerificationCode> PhoneVerificationCodesRead => Set<PhoneVerificationCode>().AsQueryable().AsNoTracking();
    
    public DbSet<UserAccess> UserAccesses => Set<UserAccess>();
    public DbSet<PhoneVerificationCode> PhoneVerificationCodes => Set<PhoneVerificationCode>();
}