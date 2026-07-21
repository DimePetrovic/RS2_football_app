namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

/// <summary>
/// The organizer published a public call for a missing player. Shown on everyone's feed;
/// a notification goes to every user not already in the match. Position is null for "any position".
/// </summary>
public sealed record PlayerWantedIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId,
    string OrganizerDisplayName,
    string? Position,
    IReadOnlyList<Guid> ParticipantUserIds,
    DateTime StartsAt,
    string? Location) : IntegrationEvent;
