namespace Comeback.Match.Application.DTOs;

public sealed record PlayerMatchHistoryItem(
    Guid MatchId,
    string Title,
    string Status,
    DateTime StartsAt,
    int? HomeScore,
    int? AwayScore,
    string Team);
