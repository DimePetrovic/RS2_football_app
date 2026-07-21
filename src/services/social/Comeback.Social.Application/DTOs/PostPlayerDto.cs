namespace Comeback.Social.Application.DTOs;

public sealed record PostPlayerDto(
    Guid UserId,
    string DisplayName,
    string? Username,
    string? AvatarUrl,
    string? Nationality,
    string Team,
    bool IsCaptain,
    int Goals,
    int Assists,
    int OwnGoals,
    decimal? OverallRating,
    decimal? GoalkeepingRating,
    decimal? DefenseRating,
    decimal? AttackRating,
    decimal? EffortRating,
    IReadOnlyList<PlayerCommentDto> Comments);
