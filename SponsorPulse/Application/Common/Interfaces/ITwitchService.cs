using SponsorPulse.Domain.Models;
using SponsorPulse.Domain.Primitives;

namespace SponsorPulse.Application.Common.Interfaces;

public interface ITwitchService
{
    Task<Result<TwitchMetrics>> GetStreamMetricsAsync(string channelName);
}
