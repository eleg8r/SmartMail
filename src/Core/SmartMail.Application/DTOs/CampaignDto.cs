using SmartMail.Domain.Enums;

namespace SmartMail.Application.DTOs;

public class CampaignDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampaignType Type { get; set; }
    public CampaignStatus Status { get; set; }
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string? TextTemplate { get; set; }
    public int TotalRecipients { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsDelivered { get; set; }
    public int EmailsOpened { get; set; }
    public int EmailsClicked { get; set; }
    public int EmailsBounced { get; set; }
    public int EmailsFailed { get; set; }
    public int Unsubscribes { get; set; }
    public int SpamComplaints { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public string? TimeZone { get; set; }
    public int? BatchSize { get; set; }
    public int? MaxEmailsPerHour { get; set; }
    public int? MaxEmailsPerDay { get; set; }
    public bool IsAbTest { get; set; }
    public bool IsDripCampaign { get; set; }
    public bool IncludeUnsubscribeLink { get; set; }
    public bool EnableOpenTracking { get; set; }
    public bool EnableClickTracking { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public double OpenRate => EmailsSent > 0 ? (double)EmailsOpened / EmailsSent * 100 : 0;
    public double ClickRate => EmailsSent > 0 ? (double)EmailsClicked / EmailsSent * 100 : 0;
    public double BounceRate => EmailsSent > 0 ? (double)EmailsBounced / EmailsSent * 100 : 0;
}

public class CreateCampaignDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampaignType Type { get; set; }
    public string From { get; set; } = string.Empty;
    public string? FromDisplayName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string? TextTemplate { get; set; }
    public List<string> Recipients { get; set; } = new();
    public List<string> RecipientListIds { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> RecipientPersonalization { get; set; } = new();
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public string? TimeZone { get; set; }
    public int? BatchSize { get; set; }
    public List<BatchScheduleDto> BatchSchedules { get; set; } = new();
    public int? MaxEmailsPerHour { get; set; }
    public int? MaxEmailsPerDay { get; set; }
    public bool EnableOpenTracking { get; set; } = true;
    public bool EnableClickTracking { get; set; } = true;
    public string? UnsubscribeUrl { get; set; }

    // A/B Testing
    public List<CampaignVariantDto>? Variants { get; set; }
    public int? AbTestSampleSize { get; set; }
    public DateTime? AbTestEndDate { get; set; }

    // Drip Campaign
    public List<DripStepDto>? DripSteps { get; set; }
}

public class BatchScheduleDto
{
    public int BatchNumber { get; set; }
    public DateTime ScheduledTime { get; set; }
    public int MaxRecipients { get; set; }
}

public class CampaignVariantDto
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public int Weight { get; set; } = 50;
}

public class DripStepDto
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public int DelayInDays { get; set; }
    public int DelayInHours { get; set; }
    public int DelayInMinutes { get; set; }
    public string? TriggerCondition { get; set; }
}

public class CampaignStatisticsDto
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsDelivered { get; set; }
    public int EmailsOpened { get; set; }
    public int EmailsClicked { get; set; }
    public int EmailsBounced { get; set; }
    public int EmailsFailed { get; set; }
    public int Unsubscribes { get; set; }
    public int SpamComplaints { get; set; }
    public double OpenRate { get; set; }
    public double ClickRate { get; set; }
    public double BounceRate { get; set; }
    public double UnsubscribeRate { get; set; }
    public List<VariantStatisticsDto> VariantStatistics { get; set; } = new();
}

public class VariantStatisticsDto
{
    public Guid VariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SentCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public double OpenRate { get; set; }
    public double ClickRate { get; set; }
}
