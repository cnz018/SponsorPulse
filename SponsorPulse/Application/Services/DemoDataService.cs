namespace SponsorPulse.Application.Services;

using Microsoft.Extensions.Options;
using SponsorPulse.Application.Common.Configuration;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Repositories;

public interface IDemoDataService
{
    Task<List<Event>> GetDemoEventsAsync();
    Task<Event?> GetDemoEventBySlugAsync(string slug);
    Task<Event?> GetDemoEventByIdAsync(Guid eventId);
    bool IsInDemoMode { get; }
}

public class DemoDataService(IOptions<DemoModeSettings> options) : IDemoDataService
{
    private readonly DemoModeSettings _settings = options.Value;

    public bool IsInDemoMode => _settings.IsDemo;

    public async Task<List<Event>> GetDemoEventsAsync()
    {
        if (_settings.IsDemo)
        {
            return await Task.FromResult(StaticDemoRepository.GetDemoEvents());
        }

        // En mode normal, ce serait les données de la base de données
        // Pour l'instant, retourner une liste vide
        return await Task.FromResult(new List<Event>());
    }

    public async Task<Event?> GetDemoEventBySlugAsync(string slug)
    {
        if (_settings.IsDemo)
        {
            var events = StaticDemoRepository.GetDemoEvents();
            var @event = events.FirstOrDefault(e => e.Slug == slug);

            return await Task.FromResult(@event);
        }

        // En mode normal, ce serait une requête DB
        return await Task.FromResult((Event?)null);
    }

    public async Task<Event?> GetDemoEventByIdAsync(Guid eventId)
    {
        if (_settings.IsDemo)
        {
            var events = StaticDemoRepository.GetDemoEvents();
            var @event = events.FirstOrDefault(e => e.Id == eventId);
            return await Task.FromResult(@event);
        }

        // En mode normal, ce serait une requête DB
        return await Task.FromResult((Event?)null);
    }
}
