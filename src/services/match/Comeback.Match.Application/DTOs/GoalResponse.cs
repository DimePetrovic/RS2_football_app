namespace Comeback.Match.Application.DTOs;

public sealed record GoalResponse(
    Guid ScorerUserId,
    string ScorerDisplayName,
    string ScoringTeam,
    bool IsOwnGoal,
    Guid? AssistUserId,
    string? AssistDisplayName);
