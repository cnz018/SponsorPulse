using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Infrastructure.Persistence;

namespace SponsorPulse.Functions;

public class TwitchTrackerFunction(ILogger<TwitchTrackerFunction> logger,
IDbContextFactory<SponsorPulseDbContext> dbContextFactory, 
ITwitchService twitchService,
IConfiguration configuration)
{
    private readonly ILogger<TwitchTrackerFunction> _logger = logger;
    private readonly IDbContextFactory<SponsorPulseDbContext> dbContextFactory = dbContextFactory;
    private readonly ITwitchService twitchService = twitchService;
    private readonly IConfiguration configuration = configuration;

    /// <summary>
    /// S'exécute automatiquement selon le planning défini par l'expression CRON.
    /// Format CRON (6 champs) : {secondes} {minutes} {heures} {jours} {mois} {jours-semaine}
    /// Ex: "*/10 * * * * *" = Toutes les 10 secondes.
    /// En production pour le live : "0 */5 * * * *" = Toutes les 5 minutes.
    /// </summary>
    [Function("TwitchTrackerFunction")]
    public async Task Run([TimerTrigger("0 */5 * * * *")])
    {
string targetChannel = configuration["Twitch_TargetChannel"] ?? "kameto";
        _logger.LogInformation("🔍 [TwitchTracker] Analyse du live pour : {Channel}", targetChannel);

        try
        {
            // 1. Récupération des métriques via ton service existant
            var result = await twitchService.GetStreamMetricsAsync(targetChannel);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogInformation("⚪ [OFFLINE] Aucun stream actif trouvé pour {Channel}.", targetChannel);
                return;
            }

            var metrics = result.Value;

            _logger.LogInformation("[LIVE] {Channel} - Spectateurs : {Viewers} | Jeu : {Game}",
                targetChannel, metrics.ViewerCount, metrics.GameName);

            // 2. Connexion à la base de données via la Factory EF Core
            using var dbContext = dbContextFactory.CreateDbContext();

            // TODO: Remplace "Events" par le nom exact de ton entité en BDD qui gère les événements/lives
            // Exemple de mise à jour ou de création d'un snapshot en base :

            var liveEvent = await dbContext.Events
            .Include(e => e.Owner)
                .FirstOrDefaultAsync(e => e.Owner.UserName == targetChannel && e.Status == Domain.Entities.EventStatus.Live);

            if (liveEvent is null)
            {
                return;
            }

            liveEvent.CurrentViewerCount = metrics.ViewerCount;
            liveEvent.LastUpdatedAt = DateTimeOffset.UtcNow;
            dbContext.Events.Update(liveEvent);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Base de données mise à jour pour le live en cours.");


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur critique lors du tracking Twitch.");
        }
    }
}