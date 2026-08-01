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
using SponsorPulse.Infrastructure.Persistence;
using SponsorPulse.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=SponsorPulse.db;Cache=Shared;Foreign Keys=False";
var analyticsConnectionString =
    builder.Configuration.GetConnectionString("SponsorPulseAnalyticsConnection")
    ?? "Data Source=sponsorpulse_analytics.db";

builder.Services.AddDbContextFactory<SponsorPulseDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddDbContext<SponsorPulseAnalyticsDbContext>(options =>
{
    options.UseSqlite(analyticsConnectionString);
});

builder.Services.AddScoped<ITwitchService, TwitchInfrastructureService>();
builder.Services.AddScoped<ITwitchAuthStateService, TwitchAuthStateService>();


builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults().UseAzureMonitorExporter();

builder.Build().Run();
