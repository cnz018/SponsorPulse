using System.Configuration;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Application.Services;
using SponsorPulse.Infrastructure.Persistence;
using SponsorPulse.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder
    .Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["Values:DefaultConnection"]
    ?? builder.Configuration["DefaultConnection"]
    ?? "Data Source=sponsorpulse.db";
var analyticsConnectionString =
    builder.Configuration.GetConnectionString("AnalyticsConnection")
    ?? builder.Configuration["Values:AnalyticsConnection"]
    ?? builder.Configuration["AnalyticsConnection"]
    ?? "Data Source=sponsorpulse_analytics.db";

builder.Services.AddDbContextFactory<SponsorPulseDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddDbContextFactory<SponsorPulseAnalyticsDbContext>(options =>
{
    options.UseSqlite(analyticsConnectionString);
});

builder.Services.AddScoped<ITwitchAuthStateService, TwitchAuthStateService>();

builder.Services.AddHttpClient<TwitchTrackingStrategy>();

builder.Services.AddTransient<IPlatformTrackingStrategy, TwitchTrackingStrategy>();

builder.Services.AddTransient<
    IPlatformTrackingStrategyResolver,
    PlatformTrackingStrategyResolver
>();

string? appInsightsConnectionString = builder.Configuration[
    "APPLICATIONINSIGHTS_CONNECTION_STRING"
];

if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    // Mode Production : Azure Monitor est configuré
    builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults().UseAzureMonitorExporter();
}
else
{
    // Mode Local : OpenTelemetry standard sans exportateur externe
    builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults();
}

builder.Build().Run();
