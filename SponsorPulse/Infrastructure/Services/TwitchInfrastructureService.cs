using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Domain.Models;
using SponsorPulse.Domain.Primitives;

namespace SponsorPulse.Infrastructure.Services;

public class TwitchInfrastructureService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TwitchInfrastructureService> logger
) : ITwitchService
{
    private const string BaseUrl = "https://api.twitch.tv/helix";
    private const string AuthUrl = "https://id.twitch.tv/oauth2/token";

    public async Task<Result<TwitchMetrics>> GetStreamMetricsAsync(string channelNameOrUrl, string? userAccessToken = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channelNameOrUrl))
            {
                logger.LogWarning("Channel name or URL cannot be empty.");
                return Result<TwitchMetrics>.Failure("Channel name or URL cannot be empty.");
            }

            var clientId = configuration["Twitch:ClientId"];
            var clientSecret = configuration["Twitch:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                logger.LogError("Twitch credentials are missing.");
                return Result<TwitchMetrics>.Failure("Configuration error. Missing credentials.");
            }

            // 1. Setup HttpClient and authentication headers
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Remove("Client-ID");
            client.DefaultRequestHeaders.Add("Client-ID", clientId);

            // If a user access token is provided (Authorization Code flow), use it to access owner-only analytics.
            if (!string.IsNullOrEmpty(userAccessToken))
            {
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userAccessToken}");
            }
            else
            {
                // App access (Client Credentials)
                using var authClient = httpClientFactory.CreateClient();
                var authResponse = await authClient.PostAsync(
                    $"{AuthUrl}?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials",
                    null
                );

                if (!authResponse.IsSuccessStatusCode)
                {
                    logger.LogError(
                        "Authentication failed. Status: {StatusCode}",
                        authResponse.StatusCode
                    );
                    return Result<TwitchMetrics>.Failure("Authentication failed.");
                }

                var authData = await authResponse.Content.ReadFromJsonAsync<TwitchAuthResponse>();
                if (string.IsNullOrEmpty(authData?.AccessToken))
                {
                    return Result<TwitchMetrics>.Failure("Failed to retrieve Access Token.");
                }

                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authData.AccessToken}");
            }

            // 3. Resolve input: can be a login, a full twitch URL, or a video id/url
            var (login, videoId) = ParseTwitchInput(channelNameOrUrl);
            string? userId = null;

            if (!string.IsNullOrEmpty(videoId))
            {
                // If we received a video id directly, fetch the video
                var directVideoReq = await client.GetAsync($"{BaseUrl}/videos?id={videoId}");
                if (directVideoReq.IsSuccessStatusCode)
                {
                    var directVideoData = await directVideoReq.Content.ReadFromJsonAsync<TwitchVideoResponse>();
                    var directVideo = directVideoData?.Data?.FirstOrDefault();
                    if (directVideo != null)
                    {
                        logger.LogInformation("Found VOD by video id for {Input}", channelNameOrUrl);
                        return Result<TwitchMetrics>.Success(
                            new TwitchMetrics
                            {
                                ViewerCount = directVideo.ViewCount,
                                PeakViewers = 0,
                                StreamDuration = ParseDuration(directVideo.Duration),
                                StartedAt = directVideo.CreatedAt,
                                GameName = "VOD Archive"
                            }
                        );
                    }
                }
                // if not found as video, continue to try as login below
            }

            if (!string.IsNullOrEmpty(login))
            {
                var userReq = await client.GetAsync($"{BaseUrl}/users?login={login}");
                if (!userReq.IsSuccessStatusCode)
                    return Result<TwitchMetrics>.Failure("User not found.");

                var userData = await userReq.Content.ReadFromJsonAsync<TwitchUserResponse>();
                userId = userData?.Data?.FirstOrDefault()?.Id;

                if (string.IsNullOrEmpty(userId))
                    return Result<TwitchMetrics>.Failure("User ID not found.");
            }

            // 4. Check for LIVE stream first
            var streamReq = await client.GetAsync($"{BaseUrl}/streams?user_id={userId}");
            if (streamReq.IsSuccessStatusCode)
            {
                var streamData = await streamReq.Content.ReadFromJsonAsync<TwitchStreamResponse>();
                var liveStream = streamData?.Data?.FirstOrDefault();

                if (liveStream != null)
                {
                    logger.LogInformation("Found LIVE stream for {Channel}", channelNameOrUrl);
                    return Result<TwitchMetrics>.Success(
                        new TwitchMetrics
                        {
                            ViewerCount = liveStream.ViewerCount,
                            PeakViewers = liveStream.ViewerCount, // Live peak is current
                            StreamDuration = DateTimeOffset.UtcNow - liveStream.StartedAt,
                            StartedAt = liveStream.StartedAt,
                            GameName = liveStream.GameName
                        }
                    );
                }
            }

            // 5. Fallback to Latest Video (VOD)
            var videoReq = await client.GetAsync($"{BaseUrl}/videos?user_id={userId}&first=1&sort=time");
            if (videoReq.IsSuccessStatusCode)
            {
                var videoData = await videoReq.Content.ReadFromJsonAsync<TwitchVideoResponse>();
                var lastVideo = videoData?.Data?.FirstOrDefault();

                if (lastVideo != null)
                {
                    logger.LogInformation("Found VOD for {Channel}", channelNameOrUrl);
                    return Result<TwitchMetrics>.Success(
                        new TwitchMetrics
                        {
                            ViewerCount = lastVideo.ViewCount, // Total views
                            PeakViewers = 0, // Not available in simple VOD endpoint
                            StreamDuration = ParseDuration(lastVideo.Duration),
                            StartedAt = lastVideo.CreatedAt,
                            GameName = "VOD Archive"
                        }
                    );
                }
            }

            return Result<TwitchMetrics>.Failure("No active stream or recent VOD found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in Twitch service.");
            return Result<TwitchMetrics>.Failure(ex.Message);
        }
    }

    private static TimeSpan ParseDuration(string duration)
    {
        // Simple parser for 1h30m20s format.
        // Using XmlConvert is a robust trick for 'PT' formatted durations, but Twitch format is slightly different (without PT)
        // However, Twitch duration is like "3h20m5s". Adding PT makes it standard ISO8601 for XmlConvert.
        try
        {
            // Normalize: 3h20m -> PT3H20M
            var normalized = "PT" + duration.ToUpper();
            return System.Xml.XmlConvert.ToTimeSpan(normalized);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static (string? login, string? videoId) ParseTwitchInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (null, null);

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            var seg = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (seg.Length >= 1)
            {
                // URL like /videos/{id}
                if (seg[0].Equals("videos", StringComparison.OrdinalIgnoreCase) && seg.Length > 1)
                    return (null, seg[1]);

                // Otherwise assume last segment is the channel login
                return (seg[^1], null);
            }

            return (null, null);
        }

        // If input is numeric, treat as video id
        if (long.TryParse(input, out _))
            return (null, input);

        return (input.Trim(), null);
    }

    // DTOs
    record TwitchAuthResponse([property: JsonPropertyName("access_token")] string AccessToken);

    record TwitchUserResponse([property: JsonPropertyName("data")] List<UserData> Data);

    record UserData([property: JsonPropertyName("id")] string Id);

    record TwitchStreamResponse([property: JsonPropertyName("data")] List<StreamData> Data);

    record StreamData(
        [property: JsonPropertyName("viewer_count")] int ViewerCount,
        [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt,
        [property: JsonPropertyName("game_name")] string GameName
    );

    record TwitchVideoResponse([property: JsonPropertyName("data")] List<VideoData> Data);

    record VideoData(
        [property: JsonPropertyName("view_count")] int ViewCount,
        [property: JsonPropertyName("duration")] string Duration,
        [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt
    );
}
