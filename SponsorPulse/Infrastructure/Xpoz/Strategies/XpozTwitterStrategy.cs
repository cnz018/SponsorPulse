using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SponsorPulse.Application.Common.Configuration;
using SponsorPulse.Domain.Enums;
using SponsorPulse.Domain.Models;
using SponsorPulse.Domain.Primitives;
using SponsorPulse.Infrastructure.Xpoz.Models;

namespace SponsorPulse.Infrastructure.Xpoz.Strategies;

public class XpozTwitterStrategy(
    HttpClient httpClient,
    IOptions<XpozSettings> xpozOptions,
    ILogger<XpozTwitterStrategy> logger
) : IXpozPlatformStrategy
{
    private const string JsonRpcVersion = "2.0";
    private const string RpcMethodName = "tools/call";
    private const string TwitterToolName = "getTwitterPostsByKeywords";
    private const string ResponseContentType = "application/json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly XpozSettings settings = xpozOptions.Value;

    public SocialPlatform Platform => SocialPlatform.Twitter;

    public async Task<Result<SocialFetchResult>> FetchAndFormatDataAsync(
        string query,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<SocialFetchResult>.Failure("La requête sociale ne peut pas être vide.");
        }

        if (startDate >= endDate)
        {
            return Result<SocialFetchResult>.Failure(
                "La date de début doit être antérieure à la date de fin."
            );
        }

        try
        {
            var response = await FetchPostsWithRetryAsync(
                query,
                startDate,
                endDate,
                cancellationToken
            );
            var posts = response.Posts.Select(MapPost).ToList();

            return Result<SocialFetchResult>.Success(
                new SocialFetchResult
                {
                    Platform = Platform,
                    Query = query,
                    Period = new DateRange
                    {
                        StartDate = ToUtc(startDate),
                        EndDate = ToUtc(endDate),
                    },
                    TotalCount = response.TotalCount > 0 ? response.TotalCount : posts.Count,
                    Posts = posts,
                }
            );
        }
        catch (OperationCanceledException exception)
        {
            logger.LogError(
                exception,
                "La récupération des publications Twitter Xpoz a été annulé."
            );

            return Result<SocialFetchResult>.Failure("La récupération Xpoz a été annulée.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Erreur pendant la récupération des publications Twitter Xpoz."
            );

            return Result<SocialFetchResult>.Failure(exception.Message);
        }
    }

    private async Task<XpozTwitterResponse> FetchPostsWithRetryAsync(
        string query,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken
    )
    {
        var maxRetries = Math.Max(1, settings.MaxRetries);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var payload = new
                {
                    jsonrpc = JsonRpcVersion,
                    method = RpcMethodName,
                    @params = new
                    {
                        name = TwitterToolName,
                        arguments = new
                        {
                            keywords = query,
                            startDate = ToUtc(startDate).ToString("O"),
                            endDate = ToUtc(endDate).ToString("O"),
                            bucket = settings.DefaultBucket,
                        },
                    },
                };

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{settings.BaseUrl.TrimEnd('/')}/{settings.TwitterSearchPath.TrimStart('/')}"
                )
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload, JsonOptions),
                        Encoding.UTF8,
                        ResponseContentType
                    ),
                };
                request.Headers.Add(settings.ApiKeyHeaderName, settings.ApiKey);

                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responsePayload = await response.Content.ReadAsStringAsync(
                        cancellationToken
                    );

                    return JsonSerializer.Deserialize<XpozTwitterResponse>(
                            responsePayload,
                            JsonOptions
                        ) ?? new XpozTwitterResponse([], 0, null);
                }

                if (!IsTransient(response.StatusCode) || attempt == maxRetries)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Xpoz a retourné {(int)response.StatusCode}: {error}",
                        null,
                        response.StatusCode
                    );
                }

                await DelayBeforeRetryAsync(attempt, response.StatusCode, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                await DelayBeforeRetryAsync(attempt, null, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "La récupération Xpoz a échoué après plusieurs tentatives."
        );
    }

    private async Task DelayBeforeRetryAsync(
        int attempt,
        HttpStatusCode? statusCode,
        CancellationToken cancellationToken
    )
    {
        var delayMs = settings.InitialRetryDelayMs * Math.Pow(2, attempt - 1);
        logger.LogWarning(
            "Nouvelle tentative Xpoz Twitter {Attempt} après HTTP {StatusCode}, délai {DelayMs} ms.",
            attempt,
            statusCode,
            delayMs
        );
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode
            is HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests
                or >= HttpStatusCode.InternalServerError;

    private static SocialPost MapPost(XpozTweet tweet) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Platform = SocialPlatform.Twitter,
            AuthorId = tweet.Author.Id,
            AuthorName = tweet.Author.DisplayName,
            AuthorHandle = tweet.Author.Username,
            AuthorFollowersCount = tweet.Author.FollowersCount,
            AuthorProfileImageUrl = tweet.Author.ProfileImageUrl,
            ContentText = tweet.Text,
            CreatedAt = ToUtc(tweet.CreatedAt),
            Url = tweet.Url ?? $"https://twitter.com/{tweet.Author.Username}/status/{tweet.Id}",
            LikesCount = tweet.Metrics.Likes,
            SharesCount = tweet.Metrics.Retweets,
            CommentsCount = tweet.Metrics.Replies,
            RawJsonPayload = JsonSerializer.Serialize(tweet, JsonOptions),
            PlatformSpecificData = new Dictionary<string, object>
            {
                ["Bookmarks"] = tweet.Metrics.Bookmarks,
                ["Language"] = tweet.Language ?? string.Empty,
            },
        };

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
