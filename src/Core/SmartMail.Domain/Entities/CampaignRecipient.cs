using SmartMail.Domain.Common;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Entities;

public class CampaignRecipient : Entity<Guid>
{
    public Guid CampaignId { get; private set; }
    public EmailAddress EmailAddress { get; private set; }
    public Dictionary<string, string> PersonalizationData { get; private set; } = new();
    public Guid? EmailId { get; private set; }
    public bool Sent { get; private set; }
    public DateTime? SentAt { get; private set; }
    public Guid? VariantId { get; private set; } // For A/B testing
    public int? DripStepIndex { get; private set; } // For drip campaigns
    public DateTime? NextScheduledDate { get; private set; } // For drip campaigns

    private CampaignRecipient() { }

    private CampaignRecipient(
        Guid id,
        Guid campaignId,
        EmailAddress emailAddress,
        Dictionary<string, string>? personalizationData = null)
    {
        Id = id;
        CampaignId = campaignId;
        EmailAddress = emailAddress;
        PersonalizationData = personalizationData ?? new Dictionary<string, string>();
        Sent = false;
    }

    public static CampaignRecipient Create(
        Guid campaignId,
        EmailAddress emailAddress,
        Dictionary<string, string>? personalizationData = null)
    {
        return new CampaignRecipient(Guid.NewGuid(), campaignId, emailAddress, personalizationData);
    }

    public void AssignVariant(Guid variantId)
    {
        VariantId = variantId;
    }

    public void MarkAsSent(Guid emailId)
    {
        Sent = true;
        SentAt = DateTime.UtcNow;
        EmailId = emailId;
    }

    public void ScheduleNextDripStep(int stepIndex, DateTime scheduledDate)
    {
        DripStepIndex = stepIndex;
        NextScheduledDate = scheduledDate;
    }

    public void AddPersonalizationData(string key, string value)
    {
        PersonalizationData[key] = value;
    }
}
