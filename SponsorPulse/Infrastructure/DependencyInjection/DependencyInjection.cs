using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SponsorPulse.Application.Common.Configuration;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Infrastructure.CloudflareR2;
using SponsorPulse.Infrastructure.Services;

namespace SponsorPulse.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpClient();
        services.AddScoped<ITwitchService, TwitchInfrastructureService>();
        services.AddScoped<ITwitterService, TwitterInfrastructureService>();
        services.AddScoped<IStaticReportService, StaticReportService>();
        services.Configure<XpozSettings>(configuration.GetSection("XpozSettings"));
        services.Configure<R2Settings>(configuration.GetSection("CloudflareR2"));
        services.AddSingleton<IR2ClientFactory, R2ClientFactory>();
        services.AddScoped<IMediaStorageService, MediaStorageService>();
        return services;
    }
}
