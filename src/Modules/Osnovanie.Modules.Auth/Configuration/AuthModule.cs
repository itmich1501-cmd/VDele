using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Features;
using Osnovanie.Modules.Auth.Infrastructure;

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

        services.AddScoped<RegisterUserHandler>();

        services.AddValidatorsFromAssembly(typeof(AuthModule).Assembly);

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        });

        services.AddIdentity<User, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>();

        return services;
    }
}