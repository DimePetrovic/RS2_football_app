namespace Comeback.BuildingBlocks.Infrastructure.Messaging;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Domain.Events;
using MassTransit;

/// <summary>
/// Shared <see cref="IIntegrationEventPublisher"/> implementation over MassTransit's
/// <see cref="IPublishEndpoint"/>. Lives in BuildingBlocks so every service registers the same
/// publisher instead of each carrying an identical copy.
/// </summary>
public sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class, IIntegrationEvent
        => _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
