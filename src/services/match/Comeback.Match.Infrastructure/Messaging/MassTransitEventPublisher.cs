namespace Comeback.Match.Infrastructure.Messaging;

using Comeback.BuildingBlocks.Domain.Events;
using Comeback.Match.Application.Common.Interfaces;
using MassTransit;

public sealed class MassTransitEventPublisher : IMatchEventPublisher
{
    private readonly IPublishEndpoint _endpoint;

    public MassTransitEventPublisher(IPublishEndpoint endpoint)
        => _endpoint = endpoint;

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : IIntegrationEvent
        => _endpoint.Publish(integrationEvent, ct);
}
