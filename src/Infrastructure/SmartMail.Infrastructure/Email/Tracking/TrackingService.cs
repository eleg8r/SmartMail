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
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly IEmailClickRepository _clickRepository;
    private readonly TrackingServiceOptions _options;
    private readonly ILogger<TrackingService> _logger;

    public TrackingService(
        IEmailRepository emailRepository,
        IEmailCampaignRepository campaignRepository,
        IEmailClickRepository clickRepository,
        IOptions<TrackingServiceOptions> options,
        ILogger<TrackingService> logger)
    {
        _emailRepository = emailRepository;
        _campaignRepository = campaignRepository;
        _clickRepository = clickRepository;
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
            var email = await _emailRepository.GetByTrackingIdAsync(trackingId, cancellationToken);

            if (email == null)
            {
                _logger.LogWarning("Email with tracking ID {TrackingId} not found", trackingId);
                return;
            }

            email.MarkAsOpened();
            await _emailRepository.UpdateAsync(email, cancellationToken);

            // Update campaign statistics if applicable
            if (email.CampaignId.HasValue)
            {
                var campaign = await _campaignRepository.GetByIdAsync(email.CampaignId.Value, cancellationToken);

                if (campaign != null)
                {
                    campaign.RecordEmailOpened();
                    await _campaignRepository.UpdateAsync(campaign, cancellationToken);
                }
            }

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
            var email = await _emailRepository.GetByIdAsync(emailId, cancellationToken);

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
            await _emailRepository.UpdateAsync(email, cancellationToken);

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

            await _clickRepository.AddAsync(emailClick, cancellationToken);

            // Update campaign statistics if applicable
            if (email.CampaignId.HasValue)
            {
                var campaign = await _campaignRepository.GetByIdAsync(email.CampaignId.Value, cancellationToken);

                if (campaign != null)
                {
                    campaign.RecordEmailClicked();
                    await _campaignRepository.UpdateAsync(campaign, cancellationToken);
                }
            }

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
            // Note: This is a simplified implementation. A production system would need
            // a method in IEmailRepository to find emails by recipient email address.
            // For now, we'll log a warning. This should be implemented in the repository.
            _logger.LogWarning("HandleBounceAsync needs repository method to find email by recipient {RecipientEmail}", recipientEmail);

            // TODO: Add GetByRecipientEmailAsync to IEmailRepository and implement it
            // var email = await _emailRepository.GetByRecipientEmailAsync(recipientEmail, cancellationToken);

            // if (email == null)
            // {
            //     _logger.LogWarning("Email for recipient {RecipientEmail} not found", recipientEmail);
            //     return;
            // }

            // email.MarkAsBounced(bounceType, reason);
            // await _emailRepository.UpdateAsync(email, cancellationToken);

            // // Update campaign statistics if applicable
            // if (email.CampaignId.HasValue)
            // {
            //     var campaign = await _campaignRepository.GetByIdAsync(email.CampaignId.Value, cancellationToken);

            //     if (campaign != null)
            //     {
            //         campaign.RecordEmailBounced();
            //         await _campaignRepository.UpdateAsync(campaign, cancellationToken);
            //     }
            // }

            // _logger.LogInformation("Recorded bounce for email {EmailId}: {BounceType} - {Reason}",
            //     email.Id, bounceType, reason);
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
            // Note: This is a simplified implementation. A production system would need
            // a method in IEmailRepository to find emails by recipient email address.
            // For now, we'll log a warning. This should be implemented in the repository.
            _logger.LogWarning("HandleSpamComplaintAsync needs repository method to find email by recipient {RecipientEmail}", recipientEmail);

            // TODO: Add GetByRecipientEmailAsync to IEmailRepository and implement it
            // var email = await _emailRepository.GetByRecipientEmailAsync(recipientEmail, cancellationToken);

            // if (email == null)
            // {
            //     _logger.LogWarning("Email for recipient {RecipientEmail} not found", recipientEmail);
            //     return;
            // }

            // email.RecordSpamComplaint();
            // await _emailRepository.UpdateAsync(email, cancellationToken);

            // // Update campaign statistics if applicable
            // if (email.CampaignId.HasValue)
            // {
            //     var campaign = await _campaignRepository.GetByIdAsync(email.CampaignId.Value, cancellationToken);

            //     if (campaign != null)
            //     {
            //         campaign.RecordSpamComplaint();
            //         await _campaignRepository.UpdateAsync(campaign, cancellationToken);
            //     }
            // }

            // _logger.LogInformation("Recorded spam complaint for email {EmailId}", email.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling spam complaint for {RecipientEmail}", recipientEmail);
        }
    }
}
