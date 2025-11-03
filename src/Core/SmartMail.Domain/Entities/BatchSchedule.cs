using SmartMail.Domain.Common;

namespace SmartMail.Domain.Entities;

public class BatchSchedule : Entity<Guid>
{
    public Guid CampaignId { get; private set; }
    public int BatchNumber { get; private set; }
    public DateTime ScheduledTime { get; private set; }
    public int MaxRecipients { get; private set; }
    public bool Completed { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int RecipientsSent { get; private set; }

    private BatchSchedule() { }

    private BatchSchedule(
        Guid id,
        Guid campaignId,
        int batchNumber,
        DateTime scheduledTime,
        int maxRecipients)
    {
        Id = id;
        CampaignId = campaignId;
        BatchNumber = batchNumber;
        ScheduledTime = scheduledTime;
        MaxRecipients = maxRecipients;
        Completed = false;
        RecipientsSent = 0;
    }

    public static BatchSchedule Create(
        Guid campaignId,
        int batchNumber,
        DateTime scheduledTime,
        int maxRecipients)
    {
        if (maxRecipients <= 0)
            throw new ArgumentException("Max recipients must be greater than 0", nameof(maxRecipients));

        return new BatchSchedule(Guid.NewGuid(), campaignId, batchNumber, scheduledTime, maxRecipients);
    }

    public void RecordRecipientSent()
    {
        RecipientsSent++;
    }

    public void MarkAsCompleted()
    {
        Completed = true;
        CompletedAt = DateTime.UtcNow;
    }

    public bool CanAcceptMore()
    {
        return !Completed && RecipientsSent < MaxRecipients;
    }
}
