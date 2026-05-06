using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.ReferenceData.Cities.Features;

namespace Osnovanie.Modules.ReferenceData;

public static class ReferenceDataModule
{
    public static IServiceCollection AddReferenceDataModule(
        this IServiceCollection services)
    {
        services.AddEndpoints(typeof(ReferenceDataModule).Assembly);

        services.AddScoped<GetCitiesHandler>();

        return services;
    }
}