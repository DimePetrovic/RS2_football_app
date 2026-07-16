namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record MatchInvitationSentIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId,
    string OrganizerDisplayName,
    Guid InviteeUserId,
    DateTime StartsAt,
    string? Location) : IntegrationEvent;
