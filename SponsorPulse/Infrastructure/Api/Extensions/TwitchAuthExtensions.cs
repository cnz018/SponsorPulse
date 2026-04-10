using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Domain.Entities;

namespace SponsorPulse.Infrastructure.Api.Extensions;

public static class TwitchAuthExtensions
{
    private const string BaseUrl = "https://api.twitch.tv/helix";
    private const string AuthUrl = "https://id.twitch.tv/oauth2/token";

    public static WebApplication MapTwitchAuthEndpoints(this WebApplication app)
    {
        var handler = new TwitchAuthHandler();

        app.MapGet("/auth/twitch/login", handler.StartLogin).WithName("TwitchLogin");
        app.MapGet("/auth/twitch/callback", handler.HandleCallback).WithName("TwitchCallback");

        return app;
    }
}

public class TwitchAuthHandler
{
    public async Task<IResult> StartLogin(
        ITwitchAuthStateService stateService,
        IConfiguration configuration
    )
    {
        var clientId = configuration["Twitch:ClientId"]; 
        var redirectUri = configuration["Twitch:RedirectUri"]; 
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return Results.BadRequest(new { error = "Missing Twitch client configuration." });

        var state = await stateService.CreateStateAsync();
        var scopes = "analytics:read:extensions analytics:read:games";
        var url = $"https://id.twitch.tv/oauth2/authorize?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes)}&state={Uri.EscapeDataString(state)}";

        return Results.Redirect(url);
    }

    public async Task<IResult> HandleCallback(
        HttpRequest request,
        IHttpClientFactory httpClientFactory,
        ITwitchAuthStateService stateService,
        IConfiguration configuration,
        ILogger<TwitchAuthHandler> logger
    )
    {
        var query = request.Query;
        var code = query["code"].FirstOrDefault();
        var state = query["state"].FirstOrDefault();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Results.BadRequest(new { error = "Missing code or state in callback." });

        var stateRecord = await stateService.FindByStateAsync(state);
        if (stateRecord == null)
            return Results.BadRequest(new { error = "Invalid state." });

        var clientId = configuration["Twitch:ClientId"];
        var clientSecret = configuration["Twitch:ClientSecret"];
        var redirectUri = configuration["Twitch:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            return Results.BadRequest(new { error = "Missing Twitch client configuration." });

        try
        {
            using var client = httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            });

            var tokenResp = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);
            if (!tokenResp.IsSuccessStatusCode)
            {
                var body = await tokenResp.Content.ReadAsStringAsync();
                logger.LogError("Token exchange failed: {Status} {Body}", tokenResp.StatusCode, body);
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            var tokenData = await tokenResp.Content.ReadFromJsonAsync<AuthorizationCodeTokenResponse>();
            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                return Results.StatusCode(StatusCodes.Status500InternalServerError);

            // Get user info to link token
            using var apiClient = httpClientFactory.CreateClient();
            apiClient.DefaultRequestHeaders.Add("Client-ID", clientId);
            apiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenData.AccessToken}");

            var userReq = await apiClient.GetAsync($"https://api.twitch.tv/helix/users");
            string? userId = null;
            if (userReq.IsSuccessStatusCode)
            {
                var userData = await userReq.Content.ReadFromJsonAsync<UserDataWrapper>();
                userId = userData?.Data?.FirstOrDefault()?.Id;
            }

            var tokenRecord = new TwitchAuthToken
            {
                State = state,
                AccessToken = tokenData.AccessToken,
                RefreshToken = tokenData.RefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenData.ExpiresIn),
                TwitchUserId = userId,
                Scopes = tokenData.Scope != null ? string.Join(' ', tokenData.Scope) : null,
            };

            await stateService.SaveAuthTokenAsync(tokenRecord);

            return Results.Ok(new { success = true, userId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in twitch callback");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    // Helper DTOs for token exchange
    private record AuthorizationCodeTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] List<string>? Scope,
        [property: JsonPropertyName("token_type")] string TokenType
    );

    private record UserDataWrapper([property: JsonPropertyName("data")] List<UserData> Data);
    private record UserData([property: JsonPropertyName("id")] string Id);
}
