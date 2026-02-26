namespace SponsorPulse.Application.Services;

using SponsorPulse.Domain.Entities;

public interface IAnalysisSimulatorService
{
    Task<AnalysisInsight> GenerateInsightAsync(Event @event);
    Task<List<SponsorRecommendation>> GetSponsorRecommendationsAsync(Event @event, int overallScore);
}

public record AnalysisInsight(
    int EngagementScore,
    int RetentionScore,
    int PeakHoursScore,
    int HashtagReachScore,
    int InfluencerImpactScore,
    string SentimentAnalysis,
    string KeyHighlight
);

public record SponsorRecommendation(
    string Category,
    int Score,
    string Description
);

public class AnalysisSimulatorService : IAnalysisSimulatorService
{
    public async Task<AnalysisInsight> GenerateInsightAsync(Event @event)
    {
        // Deterministic simulation based on event properties
        var hash = @event.Id.GetHashCode();
        var random = new Random(hash);

        var engagementScore = random.Next(60, 95);
        var retentionScore = random.Next(65, 90);
        var peakHoursScore = random.Next(55, 85);
        var hashtagReachScore = random.Next(70, 98);
        var influencerImpactScore = random.Next(60, 88);

        var sentiment = (@event.TwitterAnalytics?.TotalEngagement ?? 0) > 5000 ? "Highly Positive" : "Positive";
        var highlight = @event.ViewerCount > 50000 
            ? $"Exceptional viewership with {@event.ViewerCount:N0} concurrent viewers"
            : $"Strong engagement with {@event.TwitterAnalytics?.TotalTweets ?? 0} tweets";

        var insight = new AnalysisInsight(
            engagementScore,
            retentionScore,
            peakHoursScore,
            hashtagReachScore,
            influencerImpactScore,
            sentiment,
            highlight
        );

        return await Task.FromResult(insight);
    }

    public async Task<List<SponsorRecommendation>> GetSponsorRecommendationsAsync(Event @event, int overallScore)
    {
        return await Task.FromResult(new List<SponsorRecommendation>
        {
            new("Audience Value", (overallScore + 8) % 100, "High-quality audience demographics match sponsor profile"),
            new("Sponsorship ROI", (overallScore + 5) % 100, "Strong engagement metrics indicate good return on investment"),
            new("Growth Potential", (overallScore + 12) % 100, "Expanding reach in key markets for sponsor expansion"),
            new("Influencer Reach", (overallScore - 5) % 100, "Access to relevant influencers in target niche"),
            new("Content Quality", (overallScore + 2) % 100, "High-quality event content attracts premium sponsors"),
            new("Strategic Fit", (overallScore + 7) % 100, "Event aligns well with sponsor brand values")
        });
    }
}
