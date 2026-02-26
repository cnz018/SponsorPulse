namespace SponsorPulse.Application.Services;

using SponsorPulse.Domain.Entities;
using SponsorPulse.Application.Common.Configuration;
using Microsoft.Extensions.Options;

public interface IPdfGenerationService
{
    Task<PdfGenerationResult> GeneratePdfAsync(Event @event, PdfCustomizationOptions options);
    Task<string> GetPdfPreviewHtmlAsync(Event @event, PdfCustomizationOptions options);
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

    public async Task<PdfGenerationResult> GeneratePdfAsync(Event @event, PdfCustomizationOptions options)
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
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{options.CompanyName} - Event Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 2rem; color: #333; }}
        .header {{ border-bottom: 3px solid {options.PrimaryColor}; padding-bottom: 1rem; margin-bottom: 2rem; }}
        .company {{ font-size: 1.2rem; font-weight: bold; color: {options.PrimaryColor}; }}
        .title {{ font-size: 2rem; font-weight: bold; margin: 0.5rem 0; }}
        .metrics {{ margin-top: 2rem; }}
        .metric {{ display: inline-block; width: 45%; margin-right: 5%; margin-bottom: 1.5rem; }}
        .metric-label {{ font-size: 0.875rem; color: #666; text-transform: uppercase; }}
        .metric-value {{ font-size: 1.75rem; font-weight: bold; color: {options.SecondaryColor}; }}
        .footer {{ margin-top: 3rem; padding-top: 1rem; border-top: 1px solid #ddd; color: #666; font-size: 0.875rem; }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""company"">{options.CompanyName}</div>
        <div class=""title"">{@event.Name}</div>
        <div style=""color: #666; margin-top: 0.5rem;"">Event Report - {@event.Date:yyyy-MM-dd}</div>
    </div>
    
    <div class=""metrics"">
        <div class=""metric"">
            <div class=""metric-label"">Viewers</div>
            <div class=""metric-value"">{FormatNumber(@event.ViewerCount ?? 0)}</div>
        </div>
        <div class=""metric"">
            <div class=""metric-label"">Peak Viewers</div>
            <div class=""metric-value"">{FormatNumber(@event.PeakViewers ?? 0)}</div>
        </div>
        <div class=""metric"">
            <div class=""metric-label"">Game</div>
            <div class=""metric-value"">{@event.GameName ?? "N/A"}</div>
        </div>
        <div class=""metric"">
            <div class=""metric-label"">Platform</div>
            <div class=""metric-value"">{@event.StreamPlatform}</div>
        </div>
    </div>

    @if (@event.TwitterAnalytics != null)
    {{
        <div style=""margin-top: 2rem;"">
            <h2 style=""color: {options.PrimaryColor};"">Twitter Analytics</h2>
            <div class=""metrics"">
                <div class=""metric"">
                    <div class=""metric-label"">Total Tweets</div>
                    <div class=""metric-value"">@event.TwitterAnalytics.TotalTweets</div>
                </div>
                <div class=""metric"">
                    <div class=""metric-label"">Impressions</div>
                    <div class=""metric-value"">{FormatNumber((int)@event.TwitterAnalytics.EstimatedImpressions)}</div>
                </div>
            </div>
        </div>
    }}

    <div class=""footer"">
        <p>Generated on {@event.Date:yyyy-MM-dd} | Report ID: {@event.Id}</p>
    </div>
</body>
</html>";

        return await Task.FromResult(html);
    }

    private string FormatNumber(int value) =>
        value switch
        {
            >= 1_000_000 => $"{value / 1_000_000:F1}M",
            >= 1_000 => $"{value / 1_000:F1}K",
            _ => value.ToString()
        };
}
