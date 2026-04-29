using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Application.Common.Models;

namespace SponsorPulse.Infrastructure.Api.Extensions;

public static class StorytellingFunctionExtensions
{
    public static WebApplication MapStorytellingFunctionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/storytelling/generate", GenerateStorytelling)
            .WithName("GenerateStorytelling");

        return app;
    }

    private static async Task<IResult> GenerateStorytelling(
        StorytellingRequest request,
        IServiceProvider serviceProvider
    )
    {
        if (request is null)
            return Results.BadRequest(new { error = "Invalid request body" });

        var storytellingService = serviceProvider.GetRequiredService<IStorytellingService>();
        var response = await storytellingService.GenerateStorytellingAsync(request);

        return Results.Ok(response);
    }
}