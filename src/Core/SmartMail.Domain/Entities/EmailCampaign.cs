using SmartMail.Domain.Common;
using SmartMail.Domain.Enums;
using SmartMail.Domain.Events;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Entities;

public class EmailCampaign : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public CampaignType Type { get; private set; }
    public CampaignStatus Status { get; private set; }

    // Email Settings
    public EmailAddress FromAddress { get; private set; }
    public string Subject { get; private set; }
    public string HtmlTemplate { get; private set; }
    public string? TextTemplate { get; private set; }

    // Recipients
    public List<CampaignRecipient> Recipients { get; private set; } = new();
    public List<string> RecipientListIds { get; private set; } = new();

    // Scheduling
    public DateTime? ScheduledStartDate { get; private set; }
    public DateTime? ScheduledEndDate { get; private set; }
    public string? TimeZone { get; private set; }

    // Batching Configuration
    public int? BatchSize { get; private set; }
    public List<BatchSchedule> BatchSchedules { get; private set; } = new();

    // Rate Limiting
    public int? MaxEmailsPerHour { get; private set; }
    public int? MaxEmailsPerDay { get; private set; }

    // A/B Testing
    public bool IsAbTest { get; private set; }
    public List<CampaignVariant> Variants { get; private set; } = new();
    public int? AbTestSampleSize { get; private set; }
    public DateTime? AbTestEndDate { get; private set; }
    public Guid? WinningVariantId { get; private set; }

    // Drip Campaign
    public bool IsDripCampaign { get; private set; }
    public List<DripStep> DripSteps { get; private set; } = new();

    // Compliance
    public bool IncludeUnsubscribeLink { get; private set; }
    public string UnsubscribeUrl { get; private set; }
    public bool RequireDoubleOptIn { get; private set; }

    // Tracking
    public bool EnableOpenTracking { get; private set; }
    public bool EnableClickTracking { get; private set; }

    // Statistics
    public int TotalRecipients { get; private set; }
    public int EmailsSent { get; private set; }
    public int EmailsDelivered { get; private set; }
    public int EmailsOpened { get; private set; }
    public int EmailsClicked { get; private set; }
    public int EmailsBounced { get; private set; }
    public int EmailsFailed { get; private set; }
    public int Unsubscribes { get; private set; }
    public int SpamComplaints { get; private set; }

    // Metadata
    public Dictionary<string, string> Metadata { get; private set; } = new();
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? PausedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private EmailCampaign() { }

    private EmailCampaign(
        Guid id,
        TenantId tenantId,
        string name,
        string description,
        CampaignType type,
        EmailAddress fromAddress,
        string subject,
        string htmlTemplate,
        string? textTemplate = null)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Description = description;
        Type = type;
        FromAddress = fromAddress;
        Subject = subject;
        HtmlTemplate = htmlTemplate;
        TextTemplate = textTemplate;
        Status = CampaignStatus.Draft;
        IncludeUnsubscribeLink = true; // Default to true for compliance
        EnableOpenTracking = true;
        EnableClickTracking = true;

        AddDomainEvent(new CampaignCreatedEvent(Id, TenantId, Name));
    }

    public static EmailCampaign Create(
        TenantId tenantId,
        string name,
        string description,
        CampaignType type,
        EmailAddress fromAddress,
        string subject,
        string htmlTemplate,
        string? textTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Campaign name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject cannot be empty", nameof(subject));

        if (string.IsNullOrWhiteSpace(htmlTemplate))
            throw new ArgumentException("HTML template cannot be empty", nameof(htmlTemplate));

        return new EmailCampaign(
            Guid.NewGuid(),
            tenantId,
            name,
            description,
            type,
            fromAddress,
            subject,
            htmlTemplate,
            textTemplate);
    }

    public void AddRecipient(string email, Dictionary<string, string>? personalizationData = null)
    {
        var emailAddress = EmailAddress.Create(email);
        var recipient = CampaignRecipient.Create(Id, emailAddress, personalizationData);
        Recipients.Add(recipient);
        TotalRecipients = Recipients.Count;
    }

    public void AddRecipientList(string listId)
    {
        if (!RecipientListIds.Contains(listId))
        {
            RecipientListIds.Add(listId);
        }
    }

    public void SetSchedule(DateTime startDate, DateTime? endDate = null, string? timeZone = null)
    {
        if (startDate < DateTime.UtcNow)
            throw new ArgumentException("Start date cannot be in the past", nameof(startDate));

        if (endDate.HasValue && endDate.Value <= startDate)
            throw new ArgumentException("End date must be after start date", nameof(endDate));

        ScheduledStartDate = startDate;
        ScheduledEndDate = endDate;
        TimeZone = timeZone;
    }

    public void ConfigureBatching(int batchSize, List<BatchSchedule> schedules)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));

        BatchSize = batchSize;
        BatchSchedules = schedules;
    }

    public void SetRateLimits(int? maxPerHour = null, int? maxPerDay = null)
    {
        MaxEmailsPerHour = maxPerHour;
        MaxEmailsPerDay = maxPerDay;
    }

    public void ConfigureAbTest(List<CampaignVariant> variants, int sampleSize, DateTime endDate)
    {
        if (variants.Count < 2)
            throw new ArgumentException("A/B test requires at least 2 variants", nameof(variants));

        if (sampleSize <= 0)
            throw new ArgumentException("Sample size must be greater than 0", nameof(sampleSize));

        IsAbTest = true;
        Variants = variants;
        AbTestSampleSize = sampleSize;
        AbTestEndDate = endDate;
    }

    public void ConfigureDripCampaign(List<DripStep> steps)
    {
        if (steps.Count == 0)
            throw new ArgumentException("Drip campaign requires at least 1 step", nameof(steps));

        IsDripCampaign = true;
        DripSteps = steps;
    }

    public void SetUnsubscribeUrl(string url)
    {
        UnsubscribeUrl = url;
    }

    public void Schedule()
    {
        if (Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Only draft campaigns can be scheduled");

        if (!ScheduledStartDate.HasValue)
            throw new InvalidOperationException("Campaign must have a scheduled start date");

        if (TotalRecipients == 0 && RecipientListIds.Count == 0)
            throw new InvalidOperationException("Campaign must have recipients");

        Status = CampaignStatus.Scheduled;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CampaignScheduledEvent(Id, TenantId, ScheduledStartDate.Value));
    }

    public void Start()
    {
        if (Status != CampaignStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled campaigns can be started");

        Status = CampaignStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CampaignStartedEvent(Id, TenantId, Name));
    }

    public void Pause()
    {
        if (Status != CampaignStatus.InProgress)
            throw new InvalidOperationException("Only in-progress campaigns can be paused");

        Status = CampaignStatus.Paused;
        PausedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CampaignPausedEvent(Id, TenantId, Name));
    }

    public void Resume()
    {
        if (Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only paused campaigns can be resumed");

        Status = CampaignStatus.InProgress;
        PausedAt = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CampaignResumedEvent(Id, TenantId, Name));
    }

    public void Complete()
    {
        if (Status != CampaignStatus.InProgress)
            throw new InvalidOperationException("Only in-progress campaigns can be completed");

        Status = CampaignStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CampaignCompletedEvent(Id, TenantId, Name, TotalRecipients, EmailsSent));
    }

    public void Cancel()
    {
        if (Status == CampaignStatus.Completed || Status == CampaignStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a completed or already cancelled campaign");

        Status = CampaignStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CampaignCancelledEvent(Id, TenantId, Name));
    }

    public void RecordEmailSent()
    {
        EmailsSent++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordEmailDelivered()
    {
        EmailsDelivered++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordEmailOpened()
    {
        EmailsOpened++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordEmailClicked()
    {
        EmailsClicked++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordEmailBounced()
    {
        EmailsBounced++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordEmailFailed()
    {
        EmailsFailed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordUnsubscribe()
    {
        Unsubscribes++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSpamComplaint()
    {
        SpamComplaints++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SelectWinningVariant(Guid variantId)
    {
        if (!IsAbTest)
            throw new InvalidOperationException("Cannot select winning variant for non-A/B test campaign");

        if (!Variants.Any(v => v.Id == variantId))
            throw new ArgumentException("Variant not found", nameof(variantId));

        WinningVariantId = variantId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AbTestWinnerSelectedEvent(Id, TenantId, variantId));
    }
}
