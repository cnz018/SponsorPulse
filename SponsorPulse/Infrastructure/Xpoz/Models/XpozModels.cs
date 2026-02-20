namespace SponsorPulse.Infrastructure.Xpoz.Models;

/// <summary>
/// Modèles de réponse désérialisés depuis l'API Xpoz.ai
/// </summary>

public record XpozAuthor(
    string Id,
    string Username,
    string DisplayName,
    long FollowersCount,
    string? ProfileImageUrl
);

public record XpozMetrics(int Likes, int Retweets, int Replies, int Bookmarks);

public record XpozTweet(
    string Id,
    string Text,
    XpozAuthor Author,
    DateTime CreatedAt,
    string? Url,
    XpozMetrics Metrics,
    string? Language
);

public record XpozTwitterResponse(List<XpozTweet> Posts, int TotalCount, string? NextCursor);
