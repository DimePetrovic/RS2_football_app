namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record GroupMatchInviteIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId,
    string OrganizerDisplayName,
    string OrganizerGroupName,
    Guid CaptainUserId,
    DateTime StartsAt,
    string? Location) : IntegrationEvent;
