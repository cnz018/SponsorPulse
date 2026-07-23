using SponsorPulse.Domain.Models;

namespace SponsorPulse.Infrastructure.Repositories;

public interface ISocialPostRepository
{
    Task SavePostsAsync(
        IEnumerable<SocialPost> posts,
        CancellationToken cancellationToken = default
    );
    Task<string?> GetRawJsonForLlmAsync(
        Guid postId,
        CancellationToken cancellationToken = default
    );
}
