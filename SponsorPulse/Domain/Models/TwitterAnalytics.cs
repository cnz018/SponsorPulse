namespace SponsorPulse.Domain.Models;

/// <summary>
/// Représente un tweet du top 3 avec ses métriques principales.
/// </summary>
public record TopTweet
{
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorHandle { get; init; } = string.Empty;
    public long AuthorFollowersCount { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public int Likes { get; init; }
    public int Retweets { get; init; }
    public int Replies { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Représente un influenceur identifié comme le plus impactant sur la période.
/// </summary>
public record TopInfluencer
{
    public string Name { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public long FollowersCount { get; init; }
    public int TweetCount { get; init; }
}

/// <summary>
/// Résultats complets de l'analyse Twitter pour une période donnée et des mots-clés spécifiques.
/// </summary>
public record TwitterAnalytics
{
    public int TotalTweets { get; init; }
    public int TotalEngagement { get; init; }
    public List<TopTweet> TopTweets { get; init; } = [];
    public DateRange Period { get; init; } = new();
    public long EstimatedImpressions { get; init; }
    public decimal AdValueEquivalent { get; init; }
    public Dictionary<string, double> SentimentRatio { get; init; } = [];
    public TopInfluencer TopInfluencer { get; init; } = new();
    public double ViralMultiplier { get; init; }
}

/// <summary>
/// Représente une plage de dates.
/// </summary>
public record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}
