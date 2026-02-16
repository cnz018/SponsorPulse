namespace SponsorPulse.Domain.Models;

public record TwitchMetrics(
    int ViewerCount,
    int PeakViewers,
    TimeSpan StreamDuration,
    DateTime StartedAt,
    string GameName
);
