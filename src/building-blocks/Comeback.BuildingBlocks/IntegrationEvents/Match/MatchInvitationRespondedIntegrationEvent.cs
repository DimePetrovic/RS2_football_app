namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record MatchInvitationRespondedIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid ResponderUserId,
    string ResponderDisplayName,
    Guid OrganizerUserId,
    bool Accepted) : IntegrationEvent;
