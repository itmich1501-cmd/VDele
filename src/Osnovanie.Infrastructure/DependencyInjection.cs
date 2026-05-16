using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Osnovanie.Infrastructure.Database;
using Osnovanie.Infrastructure.Repositories.Auth;
using Osnovanie.Infrastructure.Repositories.VDele;
using Osnovanie.Infrastructure.Repositories.VLavke;
using Osnovanie.Infrastructure.Services.Auth;
using Osnovanie.Infrastructure.Services.Email;
using Osnovanie.Modules.Auth.Abstractions.Persistence;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Services;
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
        IConfiguration configuration,
        IHostEnvironment environment)
    {

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"));
            if (environment.IsDevelopment())
                options.EnableSensitiveDataLogging();
        });
        
        services.AddScoped<IAuthReadDbConnection>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IReferenceDataReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());
        
        services.Configure<MailOptions>(
            configuration.GetSection(MailOptions.SECTION_NAME));

        services.AddScoped<IEmailSender, EmailSender>();
        services.Configure<SmsIntOptions>(
            configuration.GetSection(SmsIntOptions.SECTION_NAME));

        services.AddHttpClient<ISmsSender, SmsIntSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SmsIntOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("X-Token", options.Token);
        });
        
        services.AddScoped<ITransactionManager, EfTransactionManager<AppDbContext>>();
        services.AddScoped<IPhoneVerificationCodeRepository, PhoneVerificationCodeRepository>();
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<IVDeleCustomerProfileRepository, VDeleCustomerProfileRepository>();
        services.AddScoped<IVDeleSpecialistProfileRepository, VDeleSpecialistProfileRepository>();
        services.AddScoped<IVLavkeCustomerProfileRepository, VLavkeCustomerProfileRepository>();
        services.AddScoped<IVLavkeSellerProfileRepository, VLavkeSellerProfileRepository>();
        
        services.AddScoped<IVDeleCustomersReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IVDeleSpecialistsReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IVLavkeCustomersReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IVLavkeSellersReadDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());
        
        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
