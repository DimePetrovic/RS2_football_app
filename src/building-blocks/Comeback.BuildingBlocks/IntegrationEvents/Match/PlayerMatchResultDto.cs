namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

public sealed record PlayerMatchResultDto(Guid UserId, string Team, bool IsCaptain);
