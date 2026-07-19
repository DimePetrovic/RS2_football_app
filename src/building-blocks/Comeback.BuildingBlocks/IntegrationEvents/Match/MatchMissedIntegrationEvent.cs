namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

/// <summary>The match was missed — the result was never entered (daily sweep).</summary>
public sealed record MatchMissedIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    IReadOnlyList<Guid> NotifyUserIds) : IntegrationEvent;
