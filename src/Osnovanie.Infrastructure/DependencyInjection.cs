using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Infrastructure.Database;
using Osnovanie.Infrastructure.Repositories.Auth;
using Osnovanie.Infrastructure.Repositories.VDele;
using Osnovanie.Infrastructure.Repositories.VLavke;
using Osnovanie.Infrastructure.Services.Auth;
using Osnovanie.Infrastructure.Services.Email;
using Osnovanie.Modules.Auth.Abstractions.Persistence;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.ReferenceData.Contracts;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Email;

namespace Osnovanie.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"));
            options.EnableSensitiveDataLogging();
        });
        
        services.AddScoped<IReferenceDataReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());
        
        services.Configure<MailOptions>(
            configuration.GetSection(MailOptions.SECTION_NAME));

        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ISmsSender, FakeSmsSender>();
        
        services.AddScoped<ITransactionManager, EfTransactionManager<AppDbContext>>();
        services.AddScoped<IPhoneVerificationCodeRepository, PhoneVerificationCodeRepository>();
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<IVDeleCustomerProfileRepository, VDeleCustomerProfileRepository>();
        services.AddScoped<IVDeleSpecialistProfileRepository, VDeleSpecialistProfileRepository>();
        services.AddScoped<IVLavkeSellerProfileRepository, VLavkeSellerProfileRepository>();
        
        services.AddScoped<IVDeleCustomersReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IVDeleSpecialistsReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IVLavkeCustomersReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IVLavkeSellersReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());
        
        services.AddIdentity<User, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
