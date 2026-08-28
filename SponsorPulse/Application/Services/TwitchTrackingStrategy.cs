using System.Text.Json;
using System.Text.Json.Serialization;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Application.Common.Models;
using SponsorPulse.Doimain.Enums;
using SponsorPulse.Infrastructure.Services;

namespace SponsorPulse.Application.Services;

public class TwitchTrackingStrategy(
    HttpClient httpClient,
    ITwitchAuthStateService authStateService,
    ILogger<TwitchTrackingStrategy> logger
) : IPlatformTrackingStrategy
{
    public StreamPlatform Platform => StreamPlatform.Twitch;

    private readonly HttpClient _httpClient = httpClient;
    private readonly ITwitchAuthStateService _authStateService = authStateService;
    private readonly ILogger<TwitchTrackingStrategy> _logger = logger;

    public async Task<object> CaptureAsync(string targetId)
    {
        try
        {
            // 1. Récupération dynamique du token d'accès valide
            var twitchToken = await _authStateService.FindByTwitchUserIdAsync("kenbogard");

            string accessToken = twitchToken?.AccessToken ?? "";

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Jeton d'accès Twitch introuvable ou invalide.");

                return Results.BadRequest<TwitchMetrics>(null);
            }

            // 2. Appel à l'API Twitch Helix avec le jeton frais
            var requestUrl =
                $"https://api.twitch.tv/helix/analytics/rooms/{targetId}/live_metrics?_content_type=application/json&auth_token={accessToken}";
            using var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bad Request code {statusCode}.", response.StatusCode);

                return Results.BadRequest<TwitchMetrics>(null);
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<TwitchMetrics>(responseBody);

            return Results.Ok<TwitchMetrics>(metrics);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "An error occurred during {platform} capture: {Message}",
                this.Platform,
                ex.Message
            );

            return Results.InternalServerError<TwitchMetrics>(null);
        }
    }
}
