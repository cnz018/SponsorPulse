using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SponsorPulse.Domain.Models;
using SponsorPulse.Infrastructure.Persistence;

namespace SponsorPulse.Infrastructure.Repositories;

public class SocialPostRepository(
    SponsorPulseAnalyticsDbContext dbContext,
    TimeProvider timeProvider
) : ISocialPostRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SavePostsAsync(
        IEnumerable<SocialPost> posts,
        CancellationToken cancellationToken = default
    )
    {
        var postList = posts.Where(post => post.Id != Guid.Empty).ToList();
        if (postList.Count == 0)
        {
            return;
        }

        var ids = postList.Select(post => post.Id).Distinct().ToArray();
        var existing = await dbContext
            .SocialPosts.Where(post => ids.Contains(post.Id))
            .ToDictionaryAsync(post => post.Id, cancellationToken);
        var fetchedAt = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var post in postList)
        {
            var entity = existing.GetValueOrDefault(post.Id);
            if (entity is null)
            {
                entity = new SocialPostEntity { Id = post.Id };
                dbContext.SocialPosts.Add(entity);
            }

            entity.EventId = post.EventId;
            entity.Platform = post.Platform;
            entity.AuthorId = post.AuthorId;
            entity.AuthorName = post.AuthorName;
            entity.AuthorHandle = post.AuthorHandle;
            entity.AuthorFollowersCount = post.AuthorFollowersCount;
            entity.AuthorProfileImageUrl = post.AuthorProfileImageUrl;
            entity.ContentText = post.ContentText;
            entity.CreatedAt = DateTime.SpecifyKind(post.CreatedAt, DateTimeKind.Utc);
            entity.Url = post.Url;
            entity.LikesCount = post.LikesCount;
            entity.SharesCount = post.SharesCount;
            entity.CommentsCount = post.CommentsCount;
            entity.RawJsonPayload = post.RawJsonPayload;
            entity.PlatformSpecificDataJson = JsonSerializer.Serialize(
                post.PlatformSpecificData,
                JsonOptions
            );
            entity.FetchedAt = fetchedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<string?> GetRawJsonForLlmAsync(
        Guid postId,
        CancellationToken cancellationToken = default
    ) =>
        dbContext
            .SocialPosts.Where(post => post.Id == postId)
            .Select(post => post.RawJsonPayload)
            .SingleOrDefaultAsync(cancellationToken);
}
