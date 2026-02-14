namespace SponsorPulse.Domain.Entities;

public record Event(
    Guid Id,
    string Name,
    string Description,
    DateTime Date,
    string StreamPlatform,
    string ChannelId
)
{
    public static Event Create(
        string name,
        string description,
        DateTime date,
        string platform,
        string channelId
    )
    {
        // Utilisation de GUID v7 pour des IDs triables chronologiquement
        return new Event(Guid.CreateVersion7(), name, description, date, platform, channelId);
    }
}
