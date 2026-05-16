using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VLavke.Admins.Features;
using Osnovanie.Modules.VLavke.Auth.Features;
using Osnovanie.Modules.VLavke.Customers.Features;
using Osnovanie.Modules.VLavke.Sellers.Features;

namespace Osnovanie.Modules.VLavke;

public static class VLavkeModule
{
    public static IServiceCollection AddVLavkeModule(
        this IServiceCollection services)
    {
        services.AddEndpoints(typeof(VLavkeModule).Assembly);

        services.AddScoped<RegisterVLavkeCustomerByPhoneHandler>();
        services.AddScoped<RegisterSellerByPhoneHandler>();
        services.AddScoped<VLavkeAdminLoginHandler>();

        services.AddScoped<VLavkeLoginByPhoneHandler>();
        services.AddScoped<VLavkeAuthMeHandler>();
        services.AddScoped<VLavkeGetCustomerMeHandler>();
        services.AddScoped<VLavkeGetSellerMeHandler>();
        services.AddScoped<VLavkeEditCustomerProfileHandler>();
        services.AddScoped<VLavkeEditSellerProfileHandler>();

        services.AddValidatorsFromAssembly(typeof(VLavkeModule).Assembly);

        return services;
    }
}