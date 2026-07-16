namespace Comeback.Match.Application.DTOs;

public sealed record PlayerReceivedReviewItem(
    Guid MatchId,
    string MatchTitle,
    Guid ReviewerUserId,
    string ReviewerDisplayName,
    string? ReviewerUsername,
    string? ReviewerAvatarUrl,
    string? ReviewerNationality,
    decimal OverallRating,
    decimal? GoalkeepingRating,
    decimal? DefenseRating,
    decimal? AttackRating,
    decimal? EffortRating,
    string? Comment,
    DateTime CreatedAt);
