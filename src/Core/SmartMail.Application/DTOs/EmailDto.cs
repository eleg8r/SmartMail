using SmartMail.Domain.Enums;

namespace SmartMail.Application.DTOs;

public class EmailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string From { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string ToDisplayName { get; set; } = string.Empty;
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public EmailStatus Status { get; set; }
    public Guid? CampaignId { get; set; }
    public string? TrackingId { get; set; }
    public bool OpenTracked { get; set; }
    public DateTime? OpenedAt { get; set; }
    public int OpenCount { get; set; }
    public bool ClickTracked { get; set; }
    public DateTime? FirstClickedAt { get; set; }
    public int ClickCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public BounceType? BounceType { get; set; }
    public string? BounceReason { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public EmailProviderType? ProviderUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateEmailDto
{
    public Guid TenantId { get; set; }
    public string From { get; set; } = string.Empty;
    public string? FromDisplayName { get; set; }
    public string To { get; set; } = string.Empty;
    public string? ToDisplayName { get; set; }
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public Guid? CampaignId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
}

public class AttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class SendEmailResultDto
{
    public Guid EmailId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}
