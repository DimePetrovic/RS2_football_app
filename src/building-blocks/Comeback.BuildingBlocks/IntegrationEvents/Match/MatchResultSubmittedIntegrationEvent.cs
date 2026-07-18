namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record MatchResultSubmittedIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    int HomeScore,
    int AwayScore,
    IReadOnlyList<Guid> NotifyUserIds,
    IReadOnlyList<PlayerMatchResultDto> Players,
    IReadOnlyList<ParticipantInfoDto> Participants) : IntegrationEvent;
