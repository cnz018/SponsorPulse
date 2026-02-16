using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

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
