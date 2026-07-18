namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record GroupMatchInviteRespondedIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId,
    string OpponentGroupName,
    bool Accepted) : IntegrationEvent;
