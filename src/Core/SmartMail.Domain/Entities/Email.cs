using SmartMail.Domain.Common;
using SmartMail.Domain.Enums;
using SmartMail.Domain.Events;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Entities;

public class Email : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; }
    public EmailAddress From { get; private set; }
    public EmailAddress To { get; private set; }
    public List<EmailAddress> Cc { get; private set; } = new();
    public List<EmailAddress> Bcc { get; private set; } = new();
    public EmailContent Content { get; private set; }
    public EmailStatus Status { get; private set; }
    public Guid? CampaignId { get; private set; }

    // Tracking
    public string? TrackingId { get; private set; }
    public bool OpenTracked { get; private set; }
    public DateTime? OpenedAt { get; private set; }
    public int OpenCount { get; private set; }
    public bool ClickTracked { get; private set; }
    public DateTime? FirstClickedAt { get; private set; }
    public int ClickCount { get; private set; }

    // Delivery
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? BouncedAt { get; private set; }
    public BounceType? BounceType { get; private set; }
    public string? BounceReason { get; private set; }

    // Metadata
    public Dictionary<string, string> Metadata { get; private set; } = new();
    public List<EmailAttachment> Attachments { get; private set; } = new();
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public EmailProviderType? ProviderUsed { get; private set; }

    private Email() { }

    private Email(
        Guid id,
        TenantId tenantId,
        EmailAddress from,
        EmailAddress to,
        EmailContent content,
        Guid? campaignId = null,
        DateTime? scheduledAt = null)
    {
        Id = id;
        TenantId = tenantId;
        From = from;
        To = to;
        Content = content;
        CampaignId = campaignId;
        ScheduledAt = scheduledAt;
        Status = EmailStatus.Queued;
        TrackingId = GenerateTrackingId();

        AddDomainEvent(new EmailCreatedEvent(Id, TenantId, To.Address));
    }

    public static Email Create(
        TenantId tenantId,
        EmailAddress from,
        EmailAddress to,
        EmailContent content,
        Guid? campaignId = null,
        DateTime? scheduledAt = null)
    {
        return new Email(Guid.NewGuid(), tenantId, from, to, content, campaignId, scheduledAt);
    }

    public void AddCc(EmailAddress cc)
    {
        if (!Cc.Any(x => x.Address == cc.Address))
            Cc.Add(cc);
    }

    public void AddBcc(EmailAddress bcc)
    {
        if (!Bcc.Any(x => x.Address == bcc.Address))
            Bcc.Add(bcc);
    }

    public void AddAttachment(EmailAttachment attachment)
    {
        Attachments.Add(attachment);
    }

    public void MarkAsProcessing()
    {
        Status = EmailStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSent(EmailProviderType provider)
    {
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        ProviderUsed = provider;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailSentEvent(Id, TenantId, To.Address, SentAt.Value));
    }

    public void MarkAsDelivered()
    {
        Status = EmailStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailDeliveredEvent(Id, TenantId, To.Address));
    }

    public void MarkAsOpened()
    {
        if (!OpenTracked)
        {
            OpenTracked = true;
            OpenedAt = DateTime.UtcNow;
            Status = EmailStatus.Opened;
        }

        OpenCount++;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailOpenedEvent(Id, TenantId, To.Address, CampaignId));
    }

    public void RecordClick(string url)
    {
        if (!ClickTracked)
        {
            ClickTracked = true;
            FirstClickedAt = DateTime.UtcNow;
            Status = EmailStatus.Clicked;
        }

        ClickCount++;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailClickedEvent(Id, TenantId, To.Address, url, CampaignId));
    }

    public void MarkAsBounced(BounceType bounceType, string reason)
    {
        Status = EmailStatus.Bounced;
        BouncedAt = DateTime.UtcNow;
        BounceType = bounceType;
        BounceReason = reason;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailBouncedEvent(Id, TenantId, To.Address, bounceType, reason));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailFailedEvent(Id, TenantId, To.Address, errorMessage));
    }

    public void RecordSpamComplaint()
    {
        Status = EmailStatus.SpamComplaint;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new EmailSpamComplaintEvent(Id, TenantId, To.Address));
    }

    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMetadata(string key, string value)
    {
        Metadata[key] = value;
    }

    private string GenerateTrackingId()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
