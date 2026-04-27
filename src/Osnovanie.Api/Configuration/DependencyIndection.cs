using Osnovanie.Modules.Auth.Features;
using Serilog;
using Serilog.Exceptions;

namespace Osnovanie.Api.Configuration;

public static class DependencyIndection
{
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RegisterUserHandler>();
        
        services
            .AddSerilogLogging(configuration)
            .AddOpenApiSpec();

        return services;
    }
    
    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddOpenApi();

        // если используешь SwaggerGen:
        // services.AddSwaggerGen(...);

        return services;
    }
    
    private static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "OsnovanieService"));
        
        return services;
    }
}