namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record MatchCancelledIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    IReadOnlyList<Guid> NotifyUserIds) : IntegrationEvent;
