using Microsoft.EntityFrameworkCore;
using SponsorPulse.Domain.Entities;

namespace SponsorPulse.Infrastructure.Persistence;

public class SponsorPulseDbContext(DbContextOptions<SponsorPulseDbContext> options)
    : DbContext(options)
{
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>().HasKey(e => e.Id);

        // Configuration SQLite spécifique si nécessaire
    }
}
