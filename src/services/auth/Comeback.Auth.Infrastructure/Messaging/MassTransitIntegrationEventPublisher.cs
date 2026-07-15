namespace Comeback.Auth.Infrastructure.Messaging;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Domain.Events;
using MassTransit;

internal sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
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
