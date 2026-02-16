using Microsoft.Extensions.DependencyInjection;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Infrastructure.Services;

namespace SponsorPulse.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<ITwitchService, TwitchInfrastructureService>();
        return services;
    }
}
