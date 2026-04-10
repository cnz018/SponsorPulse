using SponsorPulse.Domain.Models;
using SponsorPulse.Domain.Primitives;

namespace SponsorPulse.Application.Common.Interfaces;

public interface ITwitchService
{
    // channelNameOrUrl: can be a channel login, a full twitch URL, or a video id/url.
    // userAccessToken: optional user OAuth token (Authorization Code flow) to access owner-only analytics.
    Task<Result<TwitchMetrics>> GetStreamMetricsAsync(string channelNameOrUrl, string? userAccessToken = null);
}
