using System.Text.Json;
using System.Text.Json.Serialization;
using SponsorPulse.Application.Common.Models;
using SponsorPulse.Infrastructure.Services;

namespace SponsorPulse.Application.Services;

public class TwitchTrackingStrategy(HttpClient httpClient, string token) : IPlatformTrackingStrategy
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _token = token;

    public async Task CaptureAsync(string targetId)
    {
        try
        {
            // Appel à l'API Twitch Helix pour capturer les métriques live complexes
            var response = await _httpClient.GetAsync(
                $"https://api.twitch.tv/helix/analytics/rooms/{targetId}/live_metrics?_content_type=application/json&auth_token={_token}"
            );

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var metrics = JsonSerializer.Deserialize<TwitchMetrics>(responseBody);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
