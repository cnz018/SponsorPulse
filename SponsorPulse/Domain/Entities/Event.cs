namespace SponsorPulse.Domain.Entities;

public record Event(
    Guid Id,
    string Name,
    string Description,
    DateTime Date,
    string StreamPlatform,
    string ChannelId,
    string Slug
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
        var slug = GenerateSlug();
        return new Event(Guid.CreateVersion7(), name, description, date, platform, channelId, slug);
    }

    private static string GenerateSlug()
    {
        // Base62 encoding of milliseconds since Jan 1 2025
        var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var diff = DateTime.UtcNow - epoch;
        long value = (long)diff.TotalMilliseconds;

        if (value < 0) value = 0; // Should not happen given the epoch

        var sb = new System.Text.StringBuilder();
        do
        {
            sb.Insert(0, chars[(int)(value % 62)]);
            value /= 62;
        } while (value > 0);

        // Ensure max 7 chars (although it fits for ~100 years)
        var result = sb.ToString();
        return result.Length > 7 ? result.Substring(result.Length - 7) : result;
    }
}
