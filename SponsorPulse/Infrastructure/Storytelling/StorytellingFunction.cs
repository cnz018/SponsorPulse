using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using SponsorPulse.Application.Common.Interfaces;
using SponsorPulse.Application.Common.Models;

namespace Infrastructure;

public class StorytellingFunction(IStorytellingService storytellingService)
{
    private readonly IStorytellingService _storytellingService = storytellingService;

    [FunctionName("StorytellingFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log
    )
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<StorytellingRequest>(requestBody);

        if (request is null)
        {
            return new BadRequestObjectResult("Invalid request body");
        }

        var response = await _storytellingService.GenerateStorytellingAsync(request);

        return new OkObjectResult(response);
    }
}
