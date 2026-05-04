using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Infrastructure.DataBase;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Contracts.Services;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Features;
using Osnovanie.Modules.Auth.Infrastructure;
using Osnovanie.Modules.Auth.Infrastructure.Repositories;
using Osnovanie.Modules.Auth.Jwt;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Configuration;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(AuthModule).Assembly);

        services.AddScoped<AuthDbContext>(_ =>
            new AuthDbContext(configuration.GetConnectionString("Database")!));
        
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SECTION_NAME));
        
        services.Configure<PhoneVerificationOptions>(
            configuration.GetSection("Auth:PhoneVerification"));

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<SendPhoneCodeHandler>();
        services.AddScoped<VerifyPhoneCodeHandler>();
        
        services.AddScoped<ISmsSender, FakeSmsSender>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        
        services.AddScoped<ITransactionManager, EfTransactionManager<AuthDbContext>>();
        
        services.AddScoped<IPhoneVerificationCodeRepository, PhoneVerificationCodeRepository>();

        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        });

        services.AddIdentity<User, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}