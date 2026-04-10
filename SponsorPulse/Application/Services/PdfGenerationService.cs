namespace SponsorPulse.Application.Services;

using Microsoft.Extensions.Options;
using SponsorPulse.Application.Common.Configuration;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Domain.Models;

public interface IPdfGenerationService
{
    Task<PdfGenerationResult> GeneratePdfAsync(Event @event, PdfCustomizationOptions options);
    Task<string> GetPdfPreviewHtmlAsync(Event @event, PdfCustomizationOptions options);
    Task<ReportModel> BuildReportModelAsync(Event @event, PdfCustomizationOptions options);
    bool IsInDemoMode { get; }
}

public record PdfCustomizationOptions(
    string CompanyName,
    string LogoUrl,
    string PrimaryColor,
    string SecondaryColor
);

public record PdfGenerationResult(
    bool Success,
    byte[]? PdfBytes,
    string? ErrorMessage,
    string GeneratedAt
);

public class PdfGenerationService : IPdfGenerationService
{
    private readonly DemoModeSettings _settings;

    public bool IsInDemoMode => _settings.IsDemo;

    public PdfGenerationService(IOptions<DemoModeSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<PdfGenerationResult> GeneratePdfAsync(
        Event @event,
        PdfCustomizationOptions options
    )
    {
        try
        {
            var html = await GetPdfPreviewHtmlAsync(@event, options);

            if (_settings.IsDemo)
            {
                // Mode démo : retourner un contenu HTML encodé
                var pdfBytes = System.Text.Encoding.UTF8.GetBytes(html);

                return new PdfGenerationResult(
                    Success: true,
                    PdfBytes: pdfBytes,
                    ErrorMessage: null,
                    GeneratedAt: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                );
            }

            // Mode normal : appeler une vraie API de génération PDF (iTextSharp, Puppeteer, etc.)
            // Pour l'instant, même comportement que la démo
            var demoBytes = System.Text.Encoding.UTF8.GetBytes(html);
            return new PdfGenerationResult(
                Success: true,
                PdfBytes: demoBytes,
                ErrorMessage: null,
                GeneratedAt: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            );
        }
        catch (Exception ex)
        {
            return new PdfGenerationResult(
                Success: false,
                PdfBytes: null,
                ErrorMessage: ex.Message,
                GeneratedAt: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            );
        }
    }

    public async Task<string> GetPdfPreviewHtmlAsync(Event @event, PdfCustomizationOptions options)
    {
        var reportModel = await BuildReportModelAsync(@event, options);

        // In demo mode, we return the full HTML with the report template
        var suggestionsHtml = string.Concat(reportModel.Suggestions.Select(s => $"<li>{s}</li>"));
        var impressionsStr =
            reportModel.TwitterAnalytics?.EstimatedImpressions.ToString("N0") ?? "N/A";
        var generatedDate = DateTime.Now.ToString("dd/MM/yyyy à HH:mm");

        var html =
            $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""utf-8"" />
    <title>Rapport d'Impact Sponsoring - {options.CompanyName}</title>
    <style>
        body {{ font-family: 'Georgia', serif; margin: 0; padding: 0; background: #f5f5f5; }}
        .demo-alert {{ 
            background: #EF4444; 
            color: white; 
            padding: 1rem; 
            text-align: center; 
            font-family: 'Arial', sans-serif;
            font-weight: 600;
            font-size: 1rem;
            border-bottom: 4px solid #B91C1C;
        }}
        .report-wrapper {{ max-width: 210mm; margin: 2rem auto; background: white; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .report-content {{ padding: 40px; }}
        h1 {{ color: {reportModel.PrimaryColor}; font-size: 2.5rem; margin-bottom: 1rem; }}
        h2 {{ color: #666; font-size: 1.5rem; margin-bottom: 2rem; }}
        h3 {{ margin-top: 0; }}
        .overview-box {{ background: {reportModel.PrimaryColor}15; padding: 20px; border-radius: 12px; margin: 2rem 0; }}
        .recommendations-box {{ background: {reportModel.SecondaryColor}15; padding: 20px; border-radius: 12px; margin: 2rem 0; }}
        .footer {{ text-align: center; margin-top: 3rem; padding-top: 2rem; border-top: 2px solid #ddd; color: #999; }}
    </style>
</head>
<body>
    <div class=""demo-alert"">
        ⚠️ Ceci est un modèle de démonstration
    </div>
    <div class=""report-wrapper"">
        <div class=""report-content"">
            <h1>Rapport d'Impact Sponsoring</h1>
            <h2>{reportModel.EventName}</h2>
            
            <div class=""overview-box"">
                <h3 style=""color: {reportModel.PrimaryColor};"">Vue d'ensemble</h3>
                <p><strong>Plateforme:</strong> {reportModel.EventPlatform}</p>
                <p><strong>Date:</strong> {reportModel.EventDate:dd MMMM yyyy}</p>
                <p><strong>Impressions:</strong> {impressionsStr}</p>
            </div>
            
            <div style=""margin: 2rem 0;"">
                <h3 style=""color: {reportModel.PrimaryColor};"">Storytelling</h3>
                <p style=""line-height: 1.8; text-align: justify;"">{reportModel.StorytellingText}</p>
            </div>
            
            <div class=""recommendations-box"">
                <h3 style=""color: {reportModel.SecondaryColor};"">Recommandations</h3>
                <ul style=""line-height: 2;"">
                    {suggestionsHtml}
                </ul>
            </div>
            
            <div class=""footer"">
                <p style=""font-size: 1.5rem; font-weight: 600;"">Propulsé par SponsorPulse</p>
                <p style=""font-size: 0.875rem;"">Généré le {generatedDate}</p>
            </div>
        </div>
    </div>
</body>
</html>";

        return await Task.FromResult(html);
    }

    public async Task<ReportModel> BuildReportModelAsync(
        Event @event,
        PdfCustomizationOptions options
    )
    {
        return new ReportModel
        {
            CompanyName = options.CompanyName,
            LogoUrl = options.LogoUrl,
            PrimaryColor = options.PrimaryColor,
            SecondaryColor = options.SecondaryColor,
            EventName = @event.Name,
            EventDate = @event.Date,
            EventPlatform = @event.StreamPlatform,
            TwitchMetrics = new TwitchMetrics
            {
                ViewerCount = @event.ViewerCount ?? 0,
                PeakViewers = @event.PeakViewers ?? 0,
                StreamDuration = @event.StreamDuration ?? TimeSpan.Zero,
                StartedAt = @event.StartedAt.HasValue
                    ? new DateTimeOffset(
                        DateTime.SpecifyKind(@event.StartedAt.Value, DateTimeKind.Utc)
                    )
                    : DateTimeOffset.MinValue,
                GameName = @event.GameName ?? "N/A",
            },
            TwitterAnalytics = @event.TwitterAnalytics,
            StorytellingText = GenerateStorytellingText(@event),
            AnalysisText = GenerateAnalysisText(@event),
            OpportunitiesMissed = GenerateOpportunitiesMissed(@event),
            Suggestions = GenerateSuggestions(@event),
            PhotoUrls = @event.Media.Select(m => m.Url).Take(5).ToList(),
        };
    }

    private string GenerateStorytellingText(Event evt)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append(
            $"L'événement {evt.Name} a marqué un tournant significatif dans la stratégie de sponsoring eSport. "
        );

        if (evt.ViewerCount > 50000)
        {
            sb.Append(
                $"Avec plus de {evt.ViewerCount:N0} viewers simultanés, l'audience a démontré un engagement exceptionnel. "
            );
        }

        if (evt.TwitterAnalytics != null)
        {
            sb.Append(
                $"Sur les réseaux sociaux, {evt.TwitterAnalytics.TotalTweets:N0} tweets et {evt.TwitterAnalytics.TotalEngagement:N0} interactions "
            );
            sb.Append(
                $"ont généré une valeur publicitaire équivalente de {evt.TwitterAnalytics.AdValueEquivalent:N0}€. "
            );
        }

        sb.Append(
            $"La plateforme {evt.StreamPlatform} a été le théâtre d'une performance remarquable, "
        );
        sb.Append($"positionnant votre marque au cœur de l'expérience eSport.");

        return sb.ToString();
    }

    private string GenerateAnalysisText(Event evt)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append(
            $"Cet événement a confirmé le potentiel du sponsoring eSport comme levier de visibilité. "
        );
        sb.Append($"Les metrics Twitch montrent une audience fidèle et engagée. ");

        if (evt.PeakViewers > evt.ViewerCount * 1.5)
        {
            sb.Append(
                $"Le pic d'audience à {evt.PeakViewers:N0} viewers indique des moments forts ayant captivé l'attention. "
            );
        }

        if (evt.TwitterAnalytics != null)
        {
            sb.Append(
                $"L'impact Twitter démontre une résonance au-delà du live, avec un multiplicateur viral de {evt.TwitterAnalytics.ViralMultiplier:F2}x."
            );
        }

        return sb.ToString();
    }

    private List<string> GenerateOpportunitiesMissed(Event evt)
    {
        var opportunities = new List<string>();

        if (evt.ViewerCount < evt.PeakViewers * 0.7)
        {
            opportunities.Add(
                "Rétention audience : Optimiser le contenu pendant les creux d'audience"
            );
        }

        if (evt.TwitterAnalytics != null && evt.TwitterAnalytics.TotalTweets < 1000)
        {
            opportunities.Add(
                "Engagement Twitter : Stimuler les conversations avec des hashtags dédiés"
            );
        }

        if (evt.StreamDuration < TimeSpan.FromHours(4))
        {
            opportunities.Add(
                "Durée de stream : Étendre le temps d'antenne pour maximiser l'exposition"
            );
        }

        if (!opportunities.Any())
        {
            opportunities.Add(
                "Aucune opportunité majeure identifiée - performance globale excellente"
            );
        }

        return opportunities;
    }

    private List<string> GenerateSuggestions(Event evt)
    {
        var secondaryPlatform =
            evt.StreamPlatform == "Twitch" ? "YouTube Gaming et TikTok" : "Twitch";

        return new List<string>
        {
            "Intégrer des activations interactives pendant le stream (sondages, giveaways)",
            "Développer un contenu behind-the-scenes pour prolonger l'engagement post-événement",
            "Créer un programme d'ambassadeurs parmi les influenceurs les plus engagés",
            $"Explorer {secondaryPlatform} pour une présence multi-plateformes",
            "Mettre en place un tracking en temps réel pour optimiser les décisions pendant l'événement",
        };
    }
}
