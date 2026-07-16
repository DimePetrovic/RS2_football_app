namespace Comeback.Match.Application.DTOs;

public sealed record MatchReviewResponse(
    Guid ReviewerParticipantId,
    Guid ReviewedParticipantId,
    decimal OverallRating,
    decimal? GoalkeepingRating,
    decimal? DefenseRating,
    decimal? AttackRating,
    decimal? EffortRating,
    string? Comment,
    DateTime CreatedAt);
