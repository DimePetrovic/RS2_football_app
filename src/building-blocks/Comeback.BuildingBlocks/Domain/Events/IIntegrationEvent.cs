namespace Comeback.BuildingBlocks.Domain.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; init; }
    DateTime OccurredOn { get; init; }
    string CorrelationId { get; init; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
}
