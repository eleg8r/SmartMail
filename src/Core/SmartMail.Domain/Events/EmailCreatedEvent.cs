using SmartMail.Domain.Common;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Domain.Events;

public record EmailCreatedEvent(
    Guid EmailId,
    TenantId TenantId,
    string RecipientEmail) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}
