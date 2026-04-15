using System.Text;
using System.Text.Json;
using SponsorPulse.Application.Common.Config;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Application.Common.Models;
using SponsorPulse.Domain.Models;

namespace SponsorPulse.Application.Services;

public class StorytellingService(IHttpClientFactory httpClientFactory, LlmSettings llmSettings)
    : IStorytellingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly string _llmUrl = llmSettings.Url;
    private readonly string _apiKey = llmSettings.ApiKey;

    public async Task<StorytellingReponse> GenerateStorytellingAsync(StorytellingRequest request)
    {
        try
        {
            // Prepare request body
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add API key to headers
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);

            // Send request to LLM
            var response = await _httpClient.PostAsync(_llmUrl, content);
            response.EnsureSuccessStatusCode();

            // Parse response
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<StorytellingReponse>(responseJson);
        }
        catch (Exception ex)
        {
            // Log error and return empty response or throw
            return new StorytellingReponse { Slides = [] };
        }
    }
}
