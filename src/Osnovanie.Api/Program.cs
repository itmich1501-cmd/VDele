using System.Globalization;
using Osnovanie.Api.Configuration;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Infrastructure;
using Osnovanie.Infrastructure.Configurations;
using Osnovanie.Modules.Auth;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.ReferenceData;
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
    
    builder.Services.AddInfrastructure(builder.Configuration);
    
    var app = builder.Build();

    app.Configure();

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
