using SmartMail.Domain.Common;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Entities;

public class EmailClick : Entity<Guid>
{
    public TenantId TenantId { get; private set; }
    public Guid EmailId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public EmailAddress RecipientEmail { get; private set; }
    public string OriginalUrl { get; private set; }
    public string TrackedUrl { get; private set; }
    public DateTime ClickedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? Device { get; private set; }

    private EmailClick() { }

    private EmailClick(
        Guid id,
        TenantId tenantId,
        Guid emailId,
        Guid? campaignId,
        EmailAddress recipientEmail,
        string originalUrl,
        string trackedUrl,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        TenantId = tenantId;
        EmailId = emailId;
        CampaignId = campaignId;
        RecipientEmail = recipientEmail;
        OriginalUrl = originalUrl;
        TrackedUrl = trackedUrl;
        ClickedAt = DateTime.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public static EmailClick Create(
        TenantId tenantId,
        Guid emailId,
        Guid? campaignId,
        EmailAddress recipientEmail,
        string originalUrl,
        string trackedUrl,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
            throw new ArgumentException("Original URL cannot be empty", nameof(originalUrl));

        return new EmailClick(
            Guid.NewGuid(),
            tenantId,
            emailId,
            campaignId,
            recipientEmail,
            originalUrl,
            trackedUrl,
            ipAddress,
            userAgent);
    }

    public void SetGeolocation(string country, string city)
    {
        Country = country;
        City = city;
    }

    public void SetDevice(string device)
    {
        Device = device;
    }
}
