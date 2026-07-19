using Microsoft.EntityFrameworkCore;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Persistence; // Ton DbContext SQLite
using SponsorPulse.Models;

public static class TwitchAuthExtensions
{
    private const int DEFAUTL_TOLERANCE_MINUTES = 30;
    private static readonly string STREAM_ONLINE = "stream.online";
    private static readonly string STREAM_OFFLINE = "stream.offline";

    public static WebApplication MapTwitchWebhookEndpoints(this WebApplication app)
    {
        app.MapPost(
            "/api/webhooks/twitch",
            async (
                TwitchWebhookPayload payload,
                IDbContextFactory<SponsorPulseDbContext> dbFactory,
                IConfiguration configuration,
                ILogger<Program> logger
            ) =>
            {
                // 1. Gestion du challenge de validation de Twitch
                if (!string.IsNullOrEmpty(payload.Challenge))
                {
                    logger.LogInformation("Validation du challenge Twitch réussie.");
                    // Twitch demande de renvoyer le challenge sous forme de texte brut avec un statut 200 OK
                    return Results.Text(payload.Challenge, "text/plain");
                }

                if (string.IsNullOrEmpty(payload.Subscription?.Type) || payload.Event is null)
                {
                    logger.LogWarning(
                        "Webhook reçu pour un type d'événement inattendu ou événement null : {Type} Event: {Event}",
                        payload.Subscription?.Type,
                        payload.Event.BroadcasterUserId
                    );

                    return Results.BadRequest();
                }

                var streamEvent = payload.Event;
                if (payload.Subscription?.Type == STREAM_OFFLINE)
                {
                    logger.LogInformation(
                        "Le streamer {Name} (ID: {Id}) a coupé son live.",
                        streamEvent.BroadcasterUserName,
                        streamEvent.BroadcasterUserId
                    );

                    // On recherche l'événement qui est actuellement actif ("EnDirect") pour ce streamer
                    using var dbContext1 = await dbFactory.CreateDbContextAsync();
                    var activeEvent = await dbContext1
                        .Events.Include(e => e.Owner)
                        .Include(e => e.Owner!.LinkedAccounts)
                        .FirstOrDefaultAsync(e =>
                            e.Owner!.LinkedAccounts.Any(la =>
                                la.PlatformUserId == streamEvent.BroadcasterUserId
                            )
                            && e.Status == EventStatus.Live
                        );

                    if (activeEvent is null)
                    {
                        logger.LogWarning(
                            "Le live de {Name} s'est arrêté, mais aucun événement 'EnDirect' n'était enregistré pour lui en base.",
                            streamEvent.BroadcasterUserName
                        );

                        return Results.NotFound();
                    }

                    logger.LogInformation(
                        "Événement en direct trouvé. Clôture de l'événement '{Title}' (ID: {EventId}).",
                        activeEvent.Name,
                        activeEvent.Id
                    );

                    // On fait basculer le statut à Terminé
                    activeEvent.Status = EventStatus.Completed;

                    // Sauvegarde du changement de statut dans la base SQLite locale
                    await dbContext1.SaveChangesAsync();

                    logger.LogInformation(
                        "L'événement '{Title}' a été marqué comme Terminé avec succès.",
                        activeEvent.Name
                    );

                    return Results.Ok();
                }
                // 2. Traitement de l'événement stream.online



                logger.LogInformation(
                    "Le streamer {Name} (ID: {Id}) a lancé un live à {Time}",
                    streamEvent.BroadcasterUserName,
                    streamEvent.BroadcasterUserId,
                    streamEvent.StartedAt
                );

                // 3. Récupération de la tolérance configurée (par défaut 30 minutes si absent du appsettings)
                int toleranceMinutes = configuration.GetValue<int>(
                    "Twitch:Webhook:ToleranceMinutes",
                    DEFAUTL_TOLERANCE_MINUTES
                );

                // 4. Recherche de l'événement correspondant en base de données
                // On cherche un événement "Planifié" pour ce streamer spécifique
                using var dbContext = await dbFactory.CreateDbContextAsync();
                var candidateEvents = await dbContext
                    .Events.AsNoTracking()
                    .Include(e => e.Owner)
                    .Include(e => e.Owner.LinkedAccounts)
                    .SelectMany(
                        e => e.Owner.LinkedAccounts,
                        (e, la) => new { Event = e, LinkedAccount = la }
                    )
                    .Where(e =>
                        e.Event.StartedAt.HasValue
                        && e.Event.StartedAt.Value.Date == streamEvent.StartedAt.Date
                        && e.LinkedAccount.PlatformUserId == streamEvent.BroadcasterUserId
                        && e.Event.Status == EventStatus.Scheduled
                    )
                    .ToListAsync();

                Event? matchingEvent = null;

                foreach (var ev in candidateEvents)
                {
                    // On calcule la différence de temps absolue entre le début réel du live et l'heure planifiée
                    var timeDifference = (
                        streamEvent.StartedAt - ev.Event.StartedAt!.Value
                    ).Duration();

                    if (timeDifference.TotalMinutes <= toleranceMinutes)
                    {
                        matchingEvent = ev.Event;
                        break; // Match trouvé, on arrête la recherche
                    }
                }

                // 5. Mise à jour de l'événement s'il correspond
                if (matchingEvent is null)
                {
                    logger.LogWarning(
                        "Aucun événement planifié ne correspond au live de {Name} dans la fenêtre de +/- {Tolerance} minutes.",
                        streamEvent.BroadcasterUserName,
                        toleranceMinutes
                    );

                    return Results.Accepted(); // On retourne quand même 200 OK à Twitch pour accuser réception
                }

                logger.LogInformation(
                    "Match trouvé ! L'événement '{Title}' (ID: {EventId}) passe en direct.",
                    matchingEvent.Name,
                    matchingEvent.Id
                );

                matchingEvent.Status = EventStatus.Live;

                // On enregistre les modifications dans SQLite
                await dbContext.SaveChangesAsync();

                // TODO: Déclencher ici ton moteur de capture de statistiques toutes les 5 minutes

                // Twitch exige toujours un retour 200 OK (ou 204) rapide pour accuser réception
                return Results.Ok();
            }
        );

        return app;
    }
}
