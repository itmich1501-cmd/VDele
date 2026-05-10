using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VDele.Admins.Features;
using Osnovanie.Modules.VDele.Customers.Features;
using Osnovanie.Modules.VDele.Specialists.Features;

namespace Osnovanie.Modules.VDele;

public static class VDeleModule
{
    public static IServiceCollection AddVDeleModule(
        this IServiceCollection services)
    {
        services.AddEndpoints(typeof(VDeleModule).Assembly);

        services.AddScoped<RegisterVDeleCustomerByPhoneHandler>();
        services.AddScoped<VDeleRegisterVDeleSpecialistByPhoneHandler>();
        services.AddScoped<VDeleLoginCustomerByPhoneHandler>();
        services.AddScoped<VDeleLoginSpecialistByPhoneHandler>();
        services.AddScoped<VDeleAdminLoginHandler>();

        services.AddValidatorsFromAssembly(typeof(VDeleModule).Assembly);

        return services;
    }
}