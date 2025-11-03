using SmartMail.Domain.Common;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Events;

public record CampaignCreatedEvent(
    Guid CampaignId,
    TenantId TenantId,
    string Name) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record CampaignScheduledEvent(
    Guid CampaignId,
    TenantId TenantId,
    DateTime ScheduledStartDate) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record CampaignStartedEvent(
    Guid CampaignId,
    TenantId TenantId,
    string Name) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record CampaignPausedEvent(
    Guid CampaignId,
    TenantId TenantId,
    string Name) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record CampaignResumedEvent(
    Guid CampaignId,
    TenantId TenantId,
    string Name) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record CampaignCompletedEvent(
    Guid CampaignId,
    TenantId TenantId,
    string Name,
    int TotalRecipients,
    int EmailsSent) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record CampaignCancelledEvent(
    Guid CampaignId,
    TenantId TenantId,
    string Name) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public record AbTestWinnerSelectedEvent(
    Guid CampaignId,
    TenantId TenantId,
    Guid WinningVariantId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}
