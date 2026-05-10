using System.Globalization;
using Osnovanie.Api.Configuration;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Infrastructure;
using Osnovanie.Infrastructure.Configurations;
using Osnovanie.Modules.Auth;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.Services;
using Osnovanie.Modules.ReferenceData;
using Osnovanie.Modules.VDele;
using Osnovanie.Modules.VLavke;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");
    
    var builder = WebApplication.CreateBuilder(args);
    
    string enviroment = builder.Environment.EnvironmentName;
    
    builder.Configuration.AddJsonFile($"appsettings.{enviroment}.json", true, true);

    builder.Configuration.AddEnvironmentVariables();

    builder.Services.AddApiConfiguration(builder.Configuration);

    builder.Services.AddAuthModule(builder.Configuration);
    builder.Services.AddReferenceDataModule();
    
    
    builder.Services.AddVDeleModule();
    builder.Services.AddVLavkeModule();
    
    builder.Services.AddInfrastructure(builder.Configuration);
    
    var app = builder.Build();

    app.Configure();
    
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
        await seeder.SeedAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
