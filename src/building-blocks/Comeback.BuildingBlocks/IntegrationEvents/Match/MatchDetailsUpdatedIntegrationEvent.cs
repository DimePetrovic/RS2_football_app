namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record MatchDetailsUpdatedIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    IReadOnlyList<Guid> NotifyUserIds) : IntegrationEvent;
