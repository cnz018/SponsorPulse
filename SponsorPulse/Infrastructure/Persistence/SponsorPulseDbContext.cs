using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SponsorPulse.Domain.Entities;
using SponsorPulse.Domain.Models;
using System.Text.Json;

namespace SponsorPulse.Infrastructure.Persistence;

public class SponsorPulseDbContext(DbContextOptions<SponsorPulseDbContext> options)
    : DbContext(options)
{
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>().HasKey(e => e.Id);

        // Configure ownership of Media collection
        // Since we are using an owned entity pattern for simplicity in this context
        // Or just a separate table with FK. Here separate table is cleaner for queryability.
        modelBuilder
            .Entity<Event>()
            .HasMany(e => e.Media)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventMedia>().HasKey(m => m.Id);

        // Configure TwitterAnalytics as JSON column with value converter
        // Serializes the complex object to JSON for storage in SQLite TEXT column
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        var converter = new ValueConverter<TwitterAnalytics?, string>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonSerializerOptions),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<TwitterAnalytics>(v, jsonSerializerOptions)
        );

        modelBuilder
            .Entity<Event>()
            .Property(e => e.TwitterAnalytics)
            .HasConversion(converter)
            .HasColumnType("TEXT");

        // Configuration SQLite spécifique si nécessaire
    }
}
