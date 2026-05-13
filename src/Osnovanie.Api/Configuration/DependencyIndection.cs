using Osnovanie.Modules.Auth.Features;
using Serilog;
using Serilog.Exceptions;

namespace Osnovanie.Api.Configuration;

public static class DependencyIndection
{
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSerilogLogging(configuration)
            .AddOpenApiSpec()
            .AddCors();

        return services;
    }
    
    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddOpenApi();

        // если используешь SwaggerGen:
        // services.AddSwaggerGen(...);

        return services;
    }private static IServiceCollection AddCors(this IServiceCollection services)
    {
        services.AddCors(options => {
            options.AddDefaultPolicy(p =>
                p.WithOrigins("https://api.vdele.online", "https://vdele.online", "https://vlavke.online", "https://www.vlavke.online", "https://www.vdele.online",
                        "http://localhost:5173", "http://localhost:5174")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

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