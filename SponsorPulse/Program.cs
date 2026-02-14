using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using SponsorPulse;
using SponsorPulse.Infrastructure.Persistence;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Injection des dépendances de base
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Configuration de la base de données (Clean Architecture - Infrastructure Layer)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=SponsorPulse.db";

// Utilisation du DbContextFactory pour Blazor WASM (meilleure gestion du scope / concurrency)
builder.Services.AddDbContextFactory<SponsorPulseDbContext>(options =>
{
    // Mode "Production Distante" (abstraction prête pour l'avenir)
    // Ici, on pourrait switcher sur un provider API ou une autre config
    if (builder.HostEnvironment.IsProduction())
{
    // Mode Production : Préparation pour la logique distante (Turso via API)
    // Actuellement fallback sur SQLite local pour le MVP tant que l'API n'est pas branchée
    options.UseSqlite(connectionString);
}
else
{
    // Mode Développement : SQLite Local (WASM)
    options.UseSqlite(connectionString);
}
});

var host = builder.Build();

// Initialisation de la base de données locale au démarrage
// Note: Pas de migration pour l'instant, on utilise EnsureCreatedAsync
try
{
    var scope = host.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SponsorPulseDbContext>>();
    using var context = await dbFactory.CreateDbContextAsync();
    await context.Database.EnsureCreatedAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Erreur lors de l'initialisation de la base de données SQLite.");
}

await host.RunAsync();
