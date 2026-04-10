using SponsorPulse.Domain.Entities;

namespace SponsorPulse.Application.Common.Interfaces;

public interface ITwitchAuthStateService
{
    Task<string> CreateStateAsync();
    Task SaveAuthTokenAsync(TwitchAuthToken token);
    Task<TwitchAuthToken?> FindByStateAsync(string state);
    Task<TwitchAuthToken?> FindByTwitchUserIdAsync(string twitchUserId);
    Task<string?> GetValidAccessTokenAsync(string twitchUserId);
    Task<List<TwitchAuthToken>> ListAllAsync();
    Task RemoveByTwitchUserIdAsync(string twitchUserId);
}
