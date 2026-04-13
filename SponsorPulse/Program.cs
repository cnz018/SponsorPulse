using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using LumexUI.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SponsorPulse;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Api.Extensions;
using SponsorPulse.Infrastructure.DependencyInjection;
using SponsorPulse.Infrastructure.Persistence;
using SponsorPulse.Infrastructure.Services;
using SponsorPulse.Presentation;
using SponsorPulse.Presentation.Services;

// Initialiser SQLitePCL
SQLitePCL.Batteries_V2.Init();

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.AddDebug();

// Add Blazor Web Services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddLumexServices();

// Infrastructure Services
builder.Services.AddInfrastructure(builder.Configuration);

// Register HttpClient
builder.Services.AddHttpClient();

// Register Presigned URL API Service
builder.Services.AddScoped<PresignedUrlApiService>();

// Database Context
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=SponsorPulseV2.db";

builder.Services.AddDbContextFactory<SponsorPulseDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Identity + auth
builder
    .Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<SponsorPulseDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Minimal IdentityServer configuration (in-memory) for development
builder
    .Services.AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddAspNetIdentity<ApplicationUser>()
    .AddInMemoryIdentityResources(
        new IdentityResource[] { new IdentityResources.OpenId(), new IdentityResources.Profile() }
    )
    .AddInMemoryApiScopes(new ApiScope[] { new ApiScope("sponsor_api", "SponsorPulse API") })
    .AddInMemoryClients(
        new Client[]
        {
            new Client
            {
                ClientId = "sponsorpulse_api_client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("dev_secret".Sha256()) },
                AllowedScopes = { "sponsor_api" },
            },
        }
    )
    .AddDeveloperSigningCredential();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<
        IDbContextFactory<SponsorPulseDbContext>
    >();
    using var context = await dbFactory.CreateDbContextAsync();
    // Note: In development, if schema changes, delete the .db file and let EnsureCreatedAsync recreate it
    await context.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.UseAntiforgery();
app.MapStaticAssets();

// Map Media API endpoints (presigned URLs, etc.)
app.MapMediaPresignedUrlEndpoints();

// Map Twitch OAuth endpoints for user authorization flow
app.MapTwitchAuthEndpoints();

// Map Twitch analytics endpoints
app.MapTwitchAnalyticsEndpoints();

// Map Waitlist API endpoint
app.MapWaitlistEndpoint();

// Map Blazor Components
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
