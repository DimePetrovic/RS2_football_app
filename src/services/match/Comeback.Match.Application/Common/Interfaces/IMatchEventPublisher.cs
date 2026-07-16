namespace Comeback.Match.Application.Common.Interfaces;

using Comeback.BuildingBlocks.Domain.Events;

public interface IMatchEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : IIntegrationEvent;
}
