using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;
using System.Text.RegularExpressions;
using System.Web;

namespace SmartMail.Infrastructure.Email.Tracking;

public class TrackingServiceOptions
{
    public string BaseUrl { get; set; } = "https://track.smartmail.com";
    public string PixelEndpoint { get; set; } = "/track/open";
    public string ClickEndpoint { get; set; } = "/track/click";
}

public class TrackingService : ITrackingService
{
    private readonly IApplicationDbContext _context;
    private readonly TrackingServiceOptions _options;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        IApplicationDbContext context,
        IOptions<TrackingServiceOptions> options,
        ILogger<TrackingService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public string AddOpenTrackingPixel(string htmlContent, string trackingId)
    {
        var pixelUrl = $"{_options.BaseUrl}{_options.PixelEndpoint}?tid={trackingId}";
        var trackingPixel = $"<img src=\"{pixelUrl}\" width=\"1\" height=\"1\" style=\"display:none;\" alt=\"\" />";

        // Add pixel before closing body tag
        if (htmlContent.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return htmlContent.Replace("</body>", $"{trackingPixel}</body>", StringComparison.OrdinalIgnoreCase);
        }

        // If no body tag, append to the end
        return htmlContent + trackingPixel;
    }

    public string AddClickTracking(string htmlContent, Guid emailId, string trackingId)
    {
        // Regex to find all anchor tags with href
        var anchorRegex = new Regex(@"<a\s+(?:[^>]*?\s+)?href=""([^""]*?)""([^>]*?)>", RegexOptions.IgnoreCase);

        var result = anchorRegex.Replace(htmlContent, match =>
        {
            var originalUrl = match.Groups[1].Value;
            var otherAttributes = match.Groups[2].Value;

            // Skip if already tracked or is an anchor link
            if (originalUrl.StartsWith("#") ||
                originalUrl.Contains(_options.ClickEndpoint) ||
                originalUrl.StartsWith("mailto:"))
            {
                return match.Value;
            }

            // Create tracked URL
            var encodedUrl = HttpUtility.UrlEncode(originalUrl);
            var trackedUrl = $"{_options.BaseUrl}{_options.ClickEndpoint}?tid={trackingId}&eid={emailId}&url={encodedUrl}";

            return $"<a href=\"{trackedUrl}\"{otherAttributes}>";
        });

        return result;
    }

    public async Task RecordOpenAsync(
        string trackingId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _context.Emails
                .FirstOrDefaultAsync(e => e.TrackingId == trackingId, cancellationToken);

            if (email == null)
            {
                _logger.LogWarning("Email with tracking ID {TrackingId} not found", trackingId);
                return;
            }

            email.MarkAsOpened();

            // Update campaign statistics if applicable
            if (email.CampaignId.HasValue)
            {
                var campaign = await _context.EmailCampaigns
                    .FirstOrDefaultAsync(c => c.Id == email.CampaignId.Value, cancellationToken);

                campaign?.RecordEmailOpened();
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded open for email {EmailId}", email.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording open for tracking ID {TrackingId}", trackingId);
        }
    }

    public async Task<string> RecordClickAsync(
        Guid emailId,
        string trackedUrl,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _context.Emails
                .FirstOrDefaultAsync(e => e.Id == emailId, cancellationToken);

            if (email == null)
            {
                _logger.LogWarning("Email {EmailId} not found", emailId);
                return trackedUrl;
            }

            // Extract original URL from tracked URL
            var uri = new Uri(trackedUrl);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var originalUrl = queryParams["url"];

            if (string.IsNullOrEmpty(originalUrl))
            {
                return trackedUrl;
            }

            // Record click
            email.RecordClick(originalUrl);

            // Create EmailClick entity for detailed tracking
            var emailClick = EmailClick.Create(
                email.TenantId,
                emailId,
                email.CampaignId,
                email.To,
                originalUrl,
                trackedUrl,
                ipAddress,
                userAgent);

            _context.EmailClicks.Add(emailClick);

            // Update campaign statistics if applicable
            if (email.CampaignId.HasValue)
            {
                var campaign = await _context.EmailCampaigns
                    .FirstOrDefaultAsync(c => c.Id == email.CampaignId.Value, cancellationToken);

                campaign?.RecordEmailClicked();
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded click for email {EmailId} to {Url}", emailId, originalUrl);

            return originalUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording click for email {EmailId}", emailId);
            return trackedUrl;
        }
    }

    public async Task HandleBounceAsync(
        string recipientEmail,
        BounceType bounceType,
        string reason,
        string? providerMessageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _context.Emails
                .Where(e => e.To.Address == recipientEmail)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (email == null)
            {
                _logger.LogWarning("Email for recipient {RecipientEmail} not found", recipientEmail);
                return;
            }

            email.MarkAsBounced(bounceType, reason);

            // Update campaign statistics if applicable
            if (email.CampaignId.HasValue)
            {
                var campaign = await _context.EmailCampaigns
                    .FirstOrDefaultAsync(c => c.Id == email.CampaignId.Value, cancellationToken);

                campaign?.RecordEmailBounced();
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded bounce for email {EmailId}: {BounceType} - {Reason}",
                email.Id, bounceType, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling bounce for {RecipientEmail}", recipientEmail);
        }
    }

    public async Task HandleSpamComplaintAsync(
        string recipientEmail,
        string? providerMessageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _context.Emails
                .Where(e => e.To.Address == recipientEmail)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (email == null)
            {
                _logger.LogWarning("Email for recipient {RecipientEmail} not found", recipientEmail);
                return;
            }

            email.RecordSpamComplaint();

            // Update campaign statistics if applicable
            if (email.CampaignId.HasValue)
            {
                var campaign = await _context.EmailCampaigns
                    .FirstOrDefaultAsync(c => c.Id == email.CampaignId.Value, cancellationToken);

                campaign?.RecordSpamComplaint();
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recorded spam complaint for email {EmailId}", email.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling spam complaint for {RecipientEmail}", recipientEmail);
        }
    }
}
