using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Domain.Models;
using SponsorPulse.Domain.Primitives;

namespace SponsorPulse.Infrastructure.Services;

public class TwitchInfrastructureService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TwitchInfrastructureService> logger) : ITwitchService
{
    private const string BaseUrl = "https://api.twitch.tv/helix";
    private const string AuthUrl = "https://id.twitch.tv/oauth2/token";

    public async Task<Result<TwitchMetrics>> GetStreamMetricsAsync(string channelName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                logger.LogWarning("Channel name cannot be empty.");
                return Result<TwitchMetrics>.Failure("Channel name cannot be empty.");
            }

            var clientId = configuration["Twitch:ClientId"];
            var clientSecret = configuration["Twitch:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                logger.LogError("Twitch credentials are missing.");
                return Result<TwitchMetrics>.Failure("Configuration error. Missing credentials.");
            }

            // 1. Authenticate (Client Credentials Flow)
            using var authClient = httpClientFactory.CreateClient();
            var authResponse = await authClient.PostAsync($"{AuthUrl}?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials", null);
            
            if (!authResponse.IsSuccessStatusCode)
            {
                logger.LogError("Authentication failed. Status: {StatusCode}", authResponse.StatusCode);
                return Result<TwitchMetrics>.Failure("Authentication failed.");
            }

            var authData = await authResponse.Content.ReadFromJsonAsync<TwitchAuthResponse>();
            if (string.IsNullOrEmpty(authData?.AccessToken))
            {
                return Result<TwitchMetrics>.Failure("Failed to retrieve Access Token.");
            }

            // 2. Setup Client with Headers
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Client-ID", clientId);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authData.AccessToken}");

            // 3. Get User ID from Channel Name
            var userReq = await client.GetAsync($"{BaseUrl}/users?login={channelName}");
            if (!userReq.IsSuccessStatusCode) return Result<TwitchMetrics>.Failure("User not found.");

            var userData = await userReq.Content.ReadFromJsonAsync<TwitchUserResponse>();
            var userId = userData?.Data?.FirstOrDefault()?.Id;

            if (string.IsNullOrEmpty(userId)) return Result<TwitchMetrics>.Failure("User ID not found.");

            // 4. Check for LIVE stream first
            var streamReq = await client.GetAsync($"{BaseUrl}/streams?user_id={userId}");
            if (streamReq.IsSuccessStatusCode)
            {
                var streamData = await streamReq.Content.ReadFromJsonAsync<TwitchStreamResponse>();
                var liveStream = streamData?.Data?.FirstOrDefault();

                if (liveStream != null)
                {
                    logger.LogInformation("Found LIVE stream for {Channel}", channelName);
                    return Result<TwitchMetrics>.Success(new TwitchMetrics(
                        ViewerCount: liveStream.ViewerCount,
                        PeakViewers: liveStream.ViewerCount, // Live peak is current
                        StreamDuration: DateTime.UtcNow - liveStream.StartedAt,
                        StartedAt: liveStream.StartedAt,
                        GameName: liveStream.GameName
                    ));
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
                    logger.LogInformation("Found VOD for {Channel}", channelName);
                    return Result<TwitchMetrics>.Success(new TwitchMetrics(
                        ViewerCount: lastVideo.ViewCount, // Total views
                        PeakViewers: 0, // Not available in simple VOD endpoint
                        StreamDuration: ParseDuration(lastVideo.Duration),
                        StartedAt: lastVideo.CreatedAt,
                        GameName: "VOD Archive" 
                    ));
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

    // DTOs
    record TwitchAuthResponse([property: JsonPropertyName("access_token")] string AccessToken);
    record TwitchUserResponse([property: JsonPropertyName("data")] List<UserData> Data);
    record UserData([property: JsonPropertyName("id")] string Id);
    
    record TwitchStreamResponse([property: JsonPropertyName("data")] List<StreamData> Data);
    record StreamData(
        [property: JsonPropertyName("viewer_count")] int ViewerCount,
        [property: JsonPropertyName("started_at")] DateTime StartedAt,
        [property: JsonPropertyName("game_name")] string GameName
    );

    record TwitchVideoResponse([property: JsonPropertyName("data")] List<VideoData> Data);
    record VideoData(
        [property: JsonPropertyName("view_count")] int ViewCount,
        [property: JsonPropertyName("duration")] string Duration,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt
    );
}
