namespace SponsorPulse.Infrastructure.Repositories;

using SponsorPulse.Domain.Entities;
using SponsorPulse.Domain.Models;

/// <summary>
/// Static repository providing demo data for showcasing the application.
/// All data is deterministic and reproducible based on event ID.
/// </summary>
public static class StaticDemoRepository
{
    private static readonly Random Seeded = new(42); // Fixed seed for reproducibility

    public static List<Event> GetDemoEvents()
    {
        return new()
        {
            CreateDemoEvent(
                id: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                name: "LEC Spring Finals 2026",
                description: "League of Legends Championship Spring Finals",
                date: new DateTime(2026, 4, 15, 18, 0, 0),
                platform: "Twitch",
                channelId: "lec"
            ),
            CreateDemoEvent(
                id: Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                name: "Valorant Champions Tour",
                description: "Valorant Pro esports competition",
                date: new DateTime(2026, 3, 20, 16, 30, 0),
                platform: "Twitch",
                channelId: "valorantesports"
            ),
            CreateDemoEvent(
                id: Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
                name: "CS2 Major 2026",
                description: "Counter-Strike 2 Major Championship",
                date: new DateTime(2026, 5, 10, 14, 0, 0),
                platform: "Twitch",
                channelId: "esl_csgo"
            ),
            CreateDemoEvent(
                id: Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
                name: "Dota 2 International Qualifiers",
                description: "Path to the International",
                date: new DateTime(2026, 2, 28, 19, 0, 0),
                platform: "YouTube",
                channelId: "dotaesports"
            ),
            CreateDemoEvent(
                id: Guid.Parse("550e8400-e29b-41d4-a716-446655440005"),
                name: "PUBG Global Championship",
                description: "Battle Royale World Championship",
                date: new DateTime(2026, 6, 5, 17, 0, 0),
                platform: "Twitch",
                channelId: "pubgesports"
            )
        };
    }

    public static TwitchMetrics GetTwitchMetrics(Guid eventId)
    {
        // Deterministic random data based on eventId
        var random = new Random(eventId.GetHashCode());

        return new TwitchMetrics(
            ViewerCount: random.Next(15000, 85000),
            PeakViewers: random.Next(80000, 250000),
            StreamDuration: TimeSpan.FromHours(random.Next(4, 12)),
            StartedAt: DateTime.UtcNow.AddDays(-1),
            GameName: GetRandomGameName(random)
        );
    }

    public static TwitterAnalytics GetTwitterAnalytics(Guid eventId)
    {
        var random = new Random(eventId.GetHashCode() + 1);

        return new TwitterAnalytics
        {
            TotalTweets = random.Next(500, 2000),
            TotalEngagement = random.Next(50000, 250000),
            EstimatedImpressions = random.Next(500000, 2500000),
            AdValueEquivalent = random.Next(10000, 50000),
            TopTweets = GenerateTopTweets(random),
            TopInfluencer = GenerateTopInfluencers(random).FirstOrDefault() ?? new(),
            Period = new DateRange
            {
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            },
            SentimentRatio = new Dictionary<string, double>
            {
                { "positive", 0.72 },
                { "neutral", 0.20 },
                { "negative", 0.08 }
            },
            ViralMultiplier = 2.45
        };
    }

    private static Event CreateDemoEvent(Guid id, string name, string description, DateTime date, string platform, string channelId)
    {
        var evt = new Event(id, name, description, date, platform, channelId, GenerateSlug())
        {
            Status = EventStatus.Completed
        };

        // Populate Twitch metrics
        var twitchMetrics = GetTwitchMetrics(id);
        evt.ViewerCount = twitchMetrics.ViewerCount;
        evt.PeakViewers = twitchMetrics.PeakViewers;
        evt.StreamDuration = twitchMetrics.StreamDuration;
        evt.StartedAt = twitchMetrics.StartedAt;
        evt.GameName = twitchMetrics.GameName;

        // Populate Twitter analytics
        evt.TwitterAnalytics = GetTwitterAnalytics(id);

        return evt;
    }

    private static List<TopTweet> GenerateTopTweets(Random random)
    {
        var tweets = new List<TopTweet>
        {
            new()
            {
                AuthorName = "ProPlayer99",
                AuthorHandle = "@ProPlayer99",
                AuthorFollowersCount = 145000,
                Text = "What an incredible match! The teamwork was insane. GGs to both teams!",
                Url = "https://twitter.com/ProPlayer99/status/1234567890",
                Likes = 12450,
                Retweets = 5680,
                Replies = 2341,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                AuthorName = "EsportsCaster",
                AuthorHandle = "@EsportsCaster",
                AuthorFollowersCount = 234000,
                Text = "That play was LEGENDARY! I've never seen anything like it in 10 years of casting.",
                Url = "https://twitter.com/EsportsCaster/status/1234567891",
                Likes = 18900,
                Retweets = 8234,
                Replies = 3456,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                AuthorName = "TournamentOrg",
                AuthorHandle = "@TournamentOrg",
                AuthorFollowersCount = 567000,
                Text = "Congratulations to our champions! What a tournament this has been!",
                Url = "https://twitter.com/TournamentOrg/status/1234567892",
                Likes = 25670,
                Retweets = 12340,
                Replies = 4567,
                CreatedAt = DateTime.UtcNow
            }
        };

        return tweets;
    }

    private static List<TopInfluencer> GenerateTopInfluencers(Random random)
    {
        return new()
        {
            new()
            {
                Name = "ProStreamers",
                Handle = "@ProStreamers",
                FollowersCount = 450000,
                TweetCount = 34
            },
            new()
            {
                Name = "GamingNews",
                Handle = "@GamingNews",
                FollowersCount = 890000,
                TweetCount = 28
            },
            new()
            {
                Name = "EsportsAnalyst",
                Handle = "@EsportsAnalyst",
                FollowersCount = 320000,
                TweetCount = 21
            }
        };
    }

    private static string GetRandomGameName(Random random)
    {
        var games = new[] { "League of Legends", "Valorant", "Counter-Strike 2", "Dota 2", "PUBG", "Street Fighter 6", "Overwatch 2" };
        return games[random.Next(games.Length)];
    }

    private static string GenerateSlug()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var result = new System.Text.StringBuilder(7);
        
        for (int i = 0; i < 7; i++)
        {
            result.Append(chars[Seeded.Next(chars.Length)]);
        }
        
        return result.ToString();
    }
}
