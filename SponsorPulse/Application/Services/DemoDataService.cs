namespace SponsorPulse.Application.Services;

using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Repositories;

public interface IDemoDataService
{
    Task<List<Event>> GetDemoEventsAsync();
    Task<Event?> GetDemoEventBySlugAsync(string slug);
    Task<Event?> GetDemoEventByIdAsync(Guid eventId);
}

public class DemoDataService : IDemoDataService
{
    public async Task<List<Event>> GetDemoEventsAsync()
    {
        return await Task.FromResult(StaticDemoRepository.GetDemoEvents());
    }

    public async Task<Event?> GetDemoEventBySlugAsync(string slug)
    {
        var events = StaticDemoRepository.GetDemoEvents();
        var @event = events.FirstOrDefault(e => e.Slug == slug);
        return await Task.FromResult(@event);
    }

    public async Task<Event?> GetDemoEventByIdAsync(Guid eventId)
    {
        var events = StaticDemoRepository.GetDemoEvents();
        var @event = events.FirstOrDefault(e => e.Id == eventId);
        return await Task.FromResult(@event);
    }
}
