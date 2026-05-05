using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Infrastructure.Database;
using Osnovanie.Infrastructure.Repositories.Auth;
using Osnovanie.Infrastructure.Services.Auth;
using Osnovanie.Infrastructure.Services.Email;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Contracts.Services;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Email;

namespace Osnovanie.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AppDbContext>(_ =>
            new AppDbContext(configuration.GetConnectionString("Database")!));
        
        services.Configure<MailOptions>(
            configuration.GetSection(MailOptions.SECTION_NAME));

        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ISmsSender, FakeSmsSender>();
        
        services.AddScoped<ITransactionManager, EfTransactionManager<AppDbContext>>();
        services.AddScoped<IPhoneVerificationCodeRepository, PhoneVerificationCodeRepository>();
        
        services.AddIdentity<User, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}