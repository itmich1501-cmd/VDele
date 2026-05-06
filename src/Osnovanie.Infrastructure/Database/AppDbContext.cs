using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Infrastructure.Configurations.Auth;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.ReferenceData.Cities.Domain;

namespace Osnovanie.Infrastructure.Database;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, Osnovanie.Modules.ReferenceData.DataBase.IReferenceDataReadDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       optionsBuilder.UseLoggerFactory(CreateLoggerFactory());
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        IdentityConfiguration.ConfigureIdentity(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private ILoggerFactory CreateLoggerFactory() =>
        LoggerFactory.Create(builder => { builder.AddConsole(); });
    
    public IQueryable<UserAccess> UserAccessesRead => Set<UserAccess>().AsQueryable().AsNoTracking();
    public IQueryable<PhoneVerificationCode> PhoneVerificationCodesRead => Set<PhoneVerificationCode>().AsQueryable().AsNoTracking();
    public IQueryable<City> CitiesRead => Set<City>().AsQueryable().AsNoTracking();
    
    public DbSet<UserAccess> UserAccesses => Set<UserAccess>();
    public DbSet<PhoneVerificationCode> PhoneVerificationCodes => Set<PhoneVerificationCode>();
    public DbSet<City> Cities => Set<City>();
}