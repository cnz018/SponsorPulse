using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Persistence;

namespace SponsorPulse.Infrastructure.Services;

public class TwitchAuthStateService : ITwitchAuthStateService
{
    private readonly IDbContextFactory<SponsorPulseDbContext> _dbFactory;
    private readonly ILogger<TwitchAuthStateService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;

    public TwitchAuthStateService(
        IDbContextFactory<SponsorPulseDbContext> dbFactory,
        ILogger<TwitchAuthStateService> logger,
        IHttpClientFactory httpFactory,
        IConfiguration configuration
    )
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _httpFactory = httpFactory;
        _configuration = configuration;
    }

    public async Task<string> CreateStateAsync()
    {
        var state = Guid.NewGuid().ToString("N");
        var token = new TwitchAuthToken { State = state, CreatedAt = DateTimeOffset.UtcNow };
        using var ctx = _dbFactory.CreateDbContext();
        ctx.TwitchAuthTokens.Add(token);
        await ctx.SaveChangesAsync();
        return state;
    }

    public async Task SaveAuthTokenAsync(TwitchAuthToken token)
    {
        using var ctx = _dbFactory.CreateDbContext();
        var existing = await ctx.TwitchAuthTokens.FirstOrDefaultAsync(t => t.State == token.State);
        if (existing != null)
        {
            existing.AccessToken = token.AccessToken;
            existing.RefreshToken = token.RefreshToken;
            existing.ExpiresAt = token.ExpiresAt;
            existing.TwitchUserId = token.TwitchUserId;
            existing.Scopes = token.Scopes;
            ctx.TwitchAuthTokens.Update(existing);
        }
        else
        {
            ctx.TwitchAuthTokens.Add(token);
        }

        await ctx.SaveChangesAsync();
    }

    public async Task<TwitchAuthToken?> FindByStateAsync(string state)
    {
        using var ctx = _dbFactory.CreateDbContext();
        return await ctx.TwitchAuthTokens.FirstOrDefaultAsync(t => t.State == state);
    }

    public async Task<TwitchAuthToken?> FindByTwitchUserIdAsync(string twitchUserId)
    {
        using var ctx = _dbFactory.CreateDbContext();
        return await ctx.TwitchAuthTokens.FirstOrDefaultAsync(t => t.TwitchUserId == twitchUserId);
    }

    public async Task<List<TwitchAuthToken>> ListAllAsync()
    {
        using var ctx = _dbFactory.CreateDbContext();
        return await ctx.TwitchAuthTokens.AsNoTracking().ToListAsync();
    }

    public async Task RemoveByTwitchUserIdAsync(string twitchUserId)
    {
        using var ctx = _dbFactory.CreateDbContext();
        var existing = await ctx.TwitchAuthTokens.FirstOrDefaultAsync(t =>
            t.TwitchUserId == twitchUserId
        );
        if (existing != null)
        {
            ctx.TwitchAuthTokens.Remove(existing);
            await ctx.SaveChangesAsync();
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(string twitchUserId)
    {
        var token = await FindByTwitchUserIdAsync(twitchUserId);
        if (token == null)
            return null;

        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value > DateTimeOffset.UtcNow.AddMinutes(1))
            return token.AccessToken;

        if (string.IsNullOrEmpty(token.RefreshToken))
            return token.AccessToken; // cannot refresh

        // Attempt refresh
        try
        {
            var clientId = _configuration["Twitch:ClientId"];
            var clientSecret = _configuration["Twitch:ClientSecret"];
            using var client = _httpFactory.CreateClient();
            var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = token.RefreshToken!,
                }
            );

            var resp = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to refresh Twitch token for user {User}: {Status}",
                    twitchUserId,
                    resp.StatusCode
                );
                return token.AccessToken; // return old if present
            }

            var data = await resp.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            if (data == null || string.IsNullOrEmpty(data.AccessToken))
                return token.AccessToken;

            // Update token record
            token.AccessToken = data.AccessToken;
            token.RefreshToken = data.RefreshToken ?? token.RefreshToken;
            token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(data.ExpiresIn);

            await SaveAuthTokenAsync(token);

            return token.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception refreshing Twitch token for {User}", twitchUserId);
            return token.AccessToken;
        }
    }

    private record RefreshTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn
    );
}
