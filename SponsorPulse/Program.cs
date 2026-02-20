using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SponsorPulse;
using SponsorPulse.Infrastructure.Api.Extensions;
using SponsorPulse.Infrastructure.DependencyInjection;
using SponsorPulse.Infrastructure.Persistence;
using SponsorPulse.Presentation;
using SponsorPulse.Presentation.Services;

// Initialiser SQLitePCL
SQLitePCL.Batteries_V2.Init();

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Web Services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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

//app.UseRouting();
app.UseWebSockets();
app.UseAntiforgery();
app.MapStaticAssets();

// Map Media API endpoints (presigned URLs, etc.)
app.MapMediaPresignedUrlEndpoints();

// Map Blazor Components
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
