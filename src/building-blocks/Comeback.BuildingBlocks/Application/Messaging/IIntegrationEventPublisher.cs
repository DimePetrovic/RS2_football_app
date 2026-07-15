namespace Comeback.BuildingBlocks.Application.Messaging;

using Comeback.BuildingBlocks.Domain.Events;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent;
}
