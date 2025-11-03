using SmartMail.Domain.Enums;

namespace SmartMail.Application.Common.Interfaces;

public interface ITrackingService
{
    /// <summary>
    /// Processes email content to add tracking pixels for open tracking
    /// </summary>
    string AddOpenTrackingPixel(string htmlContent, string trackingId);

    /// <summary>
    /// Processes email content to rewrite URLs for click tracking
    /// </summary>
    string AddClickTracking(string htmlContent, Guid emailId, string trackingId);

    /// <summary>
    /// Records an email open event
    /// </summary>
    Task RecordOpenAsync(
        string trackingId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an email click event
    /// </summary>
    Task<string> RecordClickAsync(
        Guid emailId,
        string trackedUrl,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles bounce notifications from email providers
    /// </summary>
    Task HandleBounceAsync(
        string recipientEmail,
        BounceType bounceType,
        string reason,
        string? providerMessageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles spam complaint notifications
    /// </summary>
    Task HandleSpamComplaintAsync(
        string recipientEmail,
        string? providerMessageId = null,
        CancellationToken cancellationToken = default);
}
