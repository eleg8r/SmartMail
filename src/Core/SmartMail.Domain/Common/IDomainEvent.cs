namespace SmartMail.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    Guid EventId { get; }
}
