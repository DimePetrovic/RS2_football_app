namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record MatchParticipantWithdrawnIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid WithdrawnUserId,
    string WithdrawnPlayerDisplayName,
    Guid OrganizerUserId) : IntegrationEvent;
