using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Osnovanie.Infrastructure.Email;
using Osnovanie.Shared.Email;

namespace Osnovanie.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MailOptions>(
            configuration.GetSection(MailOptions.SECTION_NAME));

        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}