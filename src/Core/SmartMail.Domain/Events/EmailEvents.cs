using SmartMail.Domain.Common;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Events;

public record EmailSentEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail,
    DateTime SentAt) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record EmailDeliveredEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record EmailOpenedEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail,
    Guid? CampaignId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record EmailClickedEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail,
    string Url,
    Guid? CampaignId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record EmailBouncedEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail,
    BounceType BounceType,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record EmailFailedEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail,
    string ErrorMessage) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record EmailSpamComplaintEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}
