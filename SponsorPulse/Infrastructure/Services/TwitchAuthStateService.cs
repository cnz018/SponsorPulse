using Microsoft.EntityFrameworkCore;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Persistence;

namespace SponsorPulse.Infrastructure.Services;

public class TwitchAuthStateService : ITwitchAuthStateService
{
    private readonly IDbContextFactory<SponsorPulseDbContext> _dbFactory;
    private readonly ILogger<TwitchAuthStateService> _logger;

    public TwitchAuthStateService(
        IDbContextFactory<SponsorPulseDbContext> dbFactory,
        ILogger<TwitchAuthStateService> logger
    )
    {
        _dbFactory = dbFactory;
        _logger = logger;
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
}
