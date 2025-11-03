using SmartMail.Domain.Common;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Entities;

public class UnsubscribeRequest : Entity<Guid>
{
    public TenantId TenantId { get; private set; }
    public EmailAddress EmailAddress { get; private set; }
    public Guid? CampaignId { get; private set; }
    public Guid? EmailId { get; private set; }
    public DateTime UnsubscribedAt { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public bool GlobalUnsubscribe { get; private set; } // Unsubscribe from all campaigns

    private UnsubscribeRequest() { }

    private UnsubscribeRequest(
        Guid id,
        TenantId tenantId,
        EmailAddress emailAddress,
        Guid? campaignId,
        Guid? emailId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        bool globalUnsubscribe)
    {
        Id = id;
        TenantId = tenantId;
        EmailAddress = emailAddress;
        CampaignId = campaignId;
        EmailId = emailId;
        UnsubscribedAt = DateTime.UtcNow;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        GlobalUnsubscribe = globalUnsubscribe;
    }

    public static UnsubscribeRequest Create(
        TenantId tenantId,
        EmailAddress emailAddress,
        Guid? campaignId = null,
        Guid? emailId = null,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null,
        bool globalUnsubscribe = false)
    {
        return new UnsubscribeRequest(
            Guid.NewGuid(),
            tenantId,
            emailAddress,
            campaignId,
            emailId,
            reason,
            ipAddress,
            userAgent,
            globalUnsubscribe);
    }
}
