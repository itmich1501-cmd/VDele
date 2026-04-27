namespace Osnovanie.Api.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        // app.UseRequestCorrelationId();
        // app.UseSerilogRequestLogging();

        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "VDele API v1");
        });

        // RouteGroupBuilder apiGroup = app.MapGroup("/api/lessons").WithOpenApi();
        // app.MapEndpoints(apiGroup);

        return app;
    }
}