using Microsoft.EntityFrameworkCore;

namespace SponsorPulse.Infrastructure.Persistence;

public class SponsorPulseAnalyticsDbContext(
    DbContextOptions<SponsorPulseAnalyticsDbContext> options
) : DbContext(options)
{
    public DbSet<SocialPostEntity> SocialPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SocialPostEntity>(entity =>
        {
            entity.ToTable("SocialPosts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Platform).HasConversion<string>().IsRequired();
            entity.Property(post => post.RawJsonPayload).HasColumnType("TEXT").IsRequired();
            entity
                .Property(post => post.PlatformSpecificDataJson)
                .HasColumnType("TEXT")
                .IsRequired();
            entity.Property(post => post.FetchedAt).IsRequired();
            entity.HasIndex(post => post.Platform);
            entity.HasIndex(post => post.CreatedAt);
        });
    }
}
