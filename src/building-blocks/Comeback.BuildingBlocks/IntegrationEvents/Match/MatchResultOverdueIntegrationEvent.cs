namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

/// <summary>Result entry is officially overdue (daily sweep).</summary>
public sealed record MatchResultOverdueIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId) : IntegrationEvent;
