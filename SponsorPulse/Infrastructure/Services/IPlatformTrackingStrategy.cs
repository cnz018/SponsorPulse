namespace SponsorPulse.Infrastructure.Services;

public interface IPlatformTrackingStrategy
{
    Task CaptureAsync(string target);
}
