namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

/// <summary>A player took a free slot through the public "player wanted" call; notifies the organizer.</summary>
public sealed record PlayerJoinedViaPublicCallIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId,
    Guid PlayerUserId,
    string PlayerDisplayName) : IntegrationEvent;
