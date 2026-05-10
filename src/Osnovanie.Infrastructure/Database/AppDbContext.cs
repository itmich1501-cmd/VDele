using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Infrastructure.Configurations.Auth;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.ReferenceData.Cities.Domain;
using Osnovanie.Modules.ReferenceData.Contracts;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.Domain;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VDele.Specialists.Domain;
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Customers.Domain;
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.Domain;

namespace Osnovanie.Infrastructure.Database;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>,
    IAuthReadDbConnection,
    IReferenceDataReadDbContext,
    IVDeleCustomersReadDbContext,
    IVDeleSpecialistsReadDbContext,
    IVLavkeCustomersReadDbContext,
    IVLavkeSellersReadDbContext
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

    public DbSet<VDeleCustomerProfile> VDeleCustomerProfiles => Set<VDeleCustomerProfile>();
    public DbSet<VDeleSpecialistProfile> VDeleSpecialistProfiles => Set<VDeleSpecialistProfile>();
    public DbSet<VLavkeSellerProfile> VLavkeSellerProfiles => Set<VLavkeSellerProfile>();
    public DbSet<VLavkeCustomerProfile> VLavkeCustomerProfiles => Set<VLavkeCustomerProfile>();
    
    IQueryable<VDeleCustomerProfile> IVDeleCustomersReadDbContext.CustomerProfilesRead =>
        Set<VDeleCustomerProfile>().AsNoTracking();

    IQueryable<VDeleSpecialistProfile> IVDeleSpecialistsReadDbContext.SpecialistProfilesRead =>
        Set<VDeleSpecialistProfile>().AsNoTracking();

    IQueryable<VLavkeCustomerProfile> IVLavkeCustomersReadDbContext.CustomerProfilesRead =>
        Set<VLavkeCustomerProfile>().AsNoTracking();

    IQueryable<VLavkeSellerProfile> IVLavkeSellersReadDbContext.SellerProfilesRead =>
        Set<VLavkeSellerProfile>().AsNoTracking();
}