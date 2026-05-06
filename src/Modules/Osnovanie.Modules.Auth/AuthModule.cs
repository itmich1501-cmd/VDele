using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.Contracts.Services;
using Osnovanie.Modules.Auth.Features;
using Osnovanie.Modules.Auth.Jwt;

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

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<SendPhoneCodeHandler>();
        services.AddScoped<VerifyPhoneCodeHandler>();
        
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        
        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        });

        return services;
    }
}