using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SponsorPulse.Application.Common.Configuration;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Application.Services;
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

        // Demo Mode Configuration
        services.AddOptions<DemoModeSettings>()
            .Bind(configuration.GetSection("DemoMode"));

        // CloudMailin Waitlist Configuration
        services.AddOptions<WaitlistSettings>()
            .Bind(configuration.GetSection("CloudMailin"));

        // Application Services
        services.AddScoped<IDemoDataService, DemoDataService>();
        services.AddScoped<IAnalysisSimulatorService, AnalysisSimulatorService>();
        services.AddScoped<IPdfGenerationService, PdfGenerationService>();

        // Waitlist Service (Singleton pour le compteur)
        services.AddSingleton<IWaitlistService, WaitlistService>();

        services.Configure<XpozSettings>(configuration.GetSection("XpozSettings"));
        services.Configure<R2Settings>(configuration.GetSection("CloudflareR2"));
        services.AddSingleton<IR2ClientFactory, R2ClientFactory>();
        services.AddScoped<IMediaStorageService, MediaStorageService>();
        return services;
    }
}
