namespace SponsorPulse.Application.Services;

using SponsorPulse.Domain.Entities;
using SponsorPulse.Infrastructure.Repositories;
using SponsorPulse.Application.Common.Configuration;
using Microsoft.Extensions.Options;

public interface IDemoDataService
{
    Task<List<Event>> GetDemoEventsAsync();
    Task<Event?> GetDemoEventBySlugAsync(string slug);
    Task<Event?> GetDemoEventByIdAsync(Guid eventId);
    bool IsInDemoMode { get; }
}

public class DemoDataService : IDemoDataService
{
    private readonly DemoModeSettings _settings;

    public bool IsInDemoMode => _settings.IsDemo;

    public DemoDataService(IOptions<DemoModeSettings> options)
    {
        _settings = options.Value;
    }

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

