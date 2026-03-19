using Amazon.Runtime;
using Amazon.S3;
using Cloudflare.NET.R2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SponsorPulse.Infrastructure.CloudflareR2;

public interface IR2ClientFactory
{
    R2Client CreateClient();
    IAmazonS3 CreateS3Client();
}

public class R2ClientFactory(IOptions<R2Settings> options, ILoggerFactory loggerFactory)
    : IR2ClientFactory
{
    private readonly R2Settings _settings = options.Value;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public R2Client CreateClient()
    {
        var s3Client = CreateS3Client();
        return new R2Client(_loggerFactory, s3Client);
    }

    public IAmazonS3 CreateS3Client()
    {
        var credentials = new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true, // Cloudflare R2 requires path-style URLs
        };
        return new AmazonS3Client(credentials, config);
    }
}
