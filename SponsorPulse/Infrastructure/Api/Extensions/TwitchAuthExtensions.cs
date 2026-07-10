using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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

        // Management endpoints for connected Twitch accounts
        app.MapGet(
                "/api/twitch/accounts",
                async (ITwitchAuthStateService stateService) =>
                {
                    var list = await stateService.ListAllAsync();
                    var dto = list.Select(t => new
                        {
                            twitchUserId = t.TwitchUserId,
                            scopes = t.Scopes,
                            expiresAt = t.ExpiresAt,
                        })
                        .ToList();

                    return Results.Ok(dto);
                }
            )
            .WithName("ListTwitchAccounts");

        app.MapGet(
                "/api/linkedaccounts",
                async (Microsoft.EntityFrameworkCore.IDbContextFactory<SponsorPulse.Infrastructure.Persistence.SponsorPulseDbContext> dbFactory) =>
                {
                    await using var dbContext = await dbFactory.CreateDbContextAsync();
                    var linkedAccounts = await dbContext.LinkedAccounts
                        .Select(l => new
                        {
                            l.Id,
                            l.UserId,
                            l.Platform,
                            l.PlatformUserId,
                            l.PlatformUsername,
                            l.AccessToken,
                            l.RefreshToken,
                            l.TokenExpiresAt,
                        })
                        .ToListAsync();

                    return Results.Ok(linkedAccounts);
                }
            )
            .WithName("ListLinkedAccounts");

        app.MapDelete(
                "/api/twitch/accounts/{twitchUserId}",
                async (string twitchUserId, ITwitchAuthStateService stateService) =>
                {
                    await stateService.RemoveByTwitchUserIdAsync(twitchUserId);

                    return Results.Ok(new { success = true });
                }
            )
            .WithName("DeleteTwitchAccount");

        // Enriched details (display name, login, avatar) for UX
        app.MapGet(
                "/api/twitch/accounts/details",
                async (
                    ITwitchAuthStateService stateService,
                    IHttpClientFactory httpFactory,
                    IConfiguration configuration
                ) =>
                {
                    var list = await stateService.ListAllAsync();
                    var result = new List<object>();

                    foreach (var t in list)
                    {
                        string? displayName = null;
                        string? login = null;
                        string? profileImage = null;

                        if (!string.IsNullOrEmpty(t.TwitchUserId))
                        {
                            var token = await stateService.GetValidAccessTokenAsync(t.TwitchUserId);

                            if (!string.IsNullOrEmpty(token))
                            {
                                try
                                {
                                    using var client = httpFactory.CreateClient();

                                    client.DefaultRequestHeaders.Remove("Client-ID");
                                    client.DefaultRequestHeaders.Add(
                                        "Client-ID",
                                        configuration["Twitch:ClientId"]
                                    );
                                    client.DefaultRequestHeaders.Remove("Authorization");
                                    client.DefaultRequestHeaders.Add(
                                        "Authorization",
                                        $"Bearer {token}"
                                    );

                                    var userResp = await client.GetAsync(
                                        $"{BaseUrl}/users?id={Uri.EscapeDataString(t.TwitchUserId)}"
                                    );

                                    if (userResp.IsSuccessStatusCode)
                                    {
                                        var doc =
                                            await userResp.Content.ReadFromJsonAsync<JsonDocument>();

                                        if (
                                            doc != null
                                            && doc.RootElement.TryGetProperty("data", out var data)
                                            && data.ValueKind == JsonValueKind.Array
                                            && data.GetArrayLength() > 0
                                        )
                                        {
                                            var first = data[0];
                                            if (
                                                first.TryGetProperty("display_name", out var dn)
                                                && dn.ValueKind == JsonValueKind.String
                                            )
                                                displayName = dn.GetString();
                                            if (
                                                first.TryGetProperty("login", out var lg)
                                                && lg.ValueKind == JsonValueKind.String
                                            )
                                                login = lg.GetString();
                                            if (
                                                first.TryGetProperty(
                                                    "profile_image_url",
                                                    out var pi
                                                )
                                                && pi.ValueKind == JsonValueKind.String
                                            )
                                                profileImage = pi.GetString();
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignore profile enrich failures for UX
                                }
                            }
                        }

                        result.Add(
                            new
                            {
                                twitchUserId = t.TwitchUserId,
                                displayName,
                                login,
                                profileImageUrl = profileImage,
                                scopes = t.Scopes,
                                expiresAt = t.ExpiresAt,
                            }
                        );
                    }

                    return Results.Ok(result);
                }
            )
            .WithName("ListTwitchAccountsDetails");

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
        var url =
            $"https://id.twitch.tv/oauth2/authorize?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes)}&state={Uri.EscapeDataString(state)}";

        return Results.Redirect(url);
    }

    public async Task<IResult> HandleCallback(
        HttpRequest request,
        IHttpClientFactory httpClientFactory,
        ITwitchAuthStateService stateService,
        IConfiguration configuration,
        ILogger<TwitchAuthHandler> logger,
        Microsoft.EntityFrameworkCore.IDbContextFactory<SponsorPulse.Infrastructure.Persistence.SponsorPulseDbContext> dbFactory
    )
    {
        var query = request.Query;
        var code = query["code"].FirstOrDefault();
        var state = query["state"].FirstOrDefault();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Results.BadRequest(new { error = "Missing code or state in callback." });

        var stateRecord = await stateService.FindByStateAsync(state);

        if (stateRecord is null)
            return Results.BadRequest(new { error = "Invalid state." });

        var clientId = configuration["Twitch:ClientId"];
        var clientSecret = configuration["Twitch:ClientSecret"];
        var redirectUri = configuration["Twitch:RedirectUri"];

        if (
            string.IsNullOrEmpty(clientId)
            || string.IsNullOrEmpty(clientSecret)
            || string.IsNullOrEmpty(redirectUri)
        )
            return Results.BadRequest(new { error = "Missing Twitch client configuration." });

        try
        {
            using var client = httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = redirectUri,
                }
            );

            var tokenResp = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);

            if (!tokenResp.IsSuccessStatusCode)
            {
                var body = await tokenResp.Content.ReadAsStringAsync();

                logger.LogError(
                    "Token exchange failed: {Status} {Body}",
                    tokenResp.StatusCode,
                    body
                );
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            var tokenData =
                await tokenResp.Content.ReadFromJsonAsync<AuthorizationCodeTokenResponse>();

            if (tokenData is { AccessToken.Length: 0 })
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

            // If the user is authenticated in our app, link the Twitch token to that user
            try
            {
                var principal = request.HttpContext?.User;
                var nameId =
                    principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? principal?.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(nameId) && Guid.TryParse(nameId, out var appUserId))
                {
                    tokenRecord.UserId = appUserId;
                }
            }
            catch
            {
                // Non-fatal — linking is best-effort
            }

            await stateService.SaveAuthTokenAsync(tokenRecord);

            // Persist a LinkedAccount record for quick access to linked accounts info
            try
            {
                using var dbContext = dbFactory.CreateDbContext();

                var existing = await dbContext.LinkedAccounts.SingleOrDefaultAsync(l =>
                    l.Platform == Domain.Entities.PlatformType.Twitch && l.PlatformUserId == userId
                );

                // attempt to get username/display login from the earlier user request
                string? platformUsername = null;
                try
                {
                    if (userReq.IsSuccessStatusCode)
                    {
                        var userData = await userReq.Content.ReadFromJsonAsync<UserDataWrapper>();
                        platformUsername = userData?.Data?.FirstOrDefault()?.Id; // fallback if no login
                    }
                }
                catch { }

                if (existing is not null)
                {
                    existing.AccessToken = tokenData.AccessToken;
                    existing.RefreshToken = tokenData.RefreshToken;
                    existing.TokenExpiresAt = DateTimeOffset
                        .UtcNow.AddSeconds(tokenData.ExpiresIn)
                        .UtcDateTime;

                    if (!string.IsNullOrEmpty(platformUsername))
                        existing.PlatformUsername = platformUsername!;

                    if (!string.IsNullOrEmpty(userId))
                        existing.PlatformUserId = userId!;

                    dbContext.LinkedAccounts.Update(existing);
                }
                else
                {
                    var newLinkedAccount = new Domain.Entities.LinkedAccount
                    {
                        UserId = tokenRecord.UserId,
                        Platform = Domain.Entities.PlatformType.Twitch,
                        PlatformUserId = userId ?? string.Empty,
                        PlatformUsername = platformUsername ?? string.Empty,
                        AccessToken = tokenData.AccessToken,
                        RefreshToken = tokenData.RefreshToken,
                        TokenExpiresAt = DateTimeOffset
                            .UtcNow.AddSeconds(tokenData.ExpiresIn)
                            .UtcDateTime,
                    };

                    dbContext.LinkedAccounts.Add(newLinkedAccount);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist LinkedAccount for Twitch callback");
            }

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
