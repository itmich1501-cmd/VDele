using Osnovanie.Api.Middlewares;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Framework.MIddlewares;
using Serilog;

namespace Osnovanie.Api.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseRequestCorrelationId();
        app.UseExceptionMiddleware();
        app.UseSerilogRequestLogging();

        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "VDele API v1");
        });
        
        var apiGroup = app.MapGroup("api");

        app.MapEndpoints(apiGroup);

        return app;
    }
}