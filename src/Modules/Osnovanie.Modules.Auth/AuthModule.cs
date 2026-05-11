using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.Features;
using Osnovanie.Modules.Auth.Jwt;
using Osnovanie.Modules.Auth.Services;

namespace Osnovanie.Modules.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(AuthModule).Assembly);
        
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SECTION_NAME));
        
        services.Configure<PhoneVerificationOptions>(
            configuration.GetSection("Auth:PhoneVerification"));
        
        services.Configure<AdminSeedOptions>(
            configuration.GetSection(AdminSeedOptions.SECTION_NAME));

        services.AddScoped<AdminSeeder>();
        
        services.AddScoped<SendPhoneCodeHandler>();
        services.AddScoped<VerifyPhoneCodeHandler>();
        
        services.AddScoped<AuthRegistrationService>();
        
        services.AddScoped<IAuthRegistrationService, AuthRegistrationService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IAuthLoginService, AuthLoginService>();
        
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        
        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredLength = 1;
            options.User.RequireUniqueEmail = false;
        });

        return services;
    }
}