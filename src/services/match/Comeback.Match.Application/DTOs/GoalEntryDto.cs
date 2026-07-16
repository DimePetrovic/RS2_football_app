namespace Comeback.Match.Application.DTOs;

public sealed record GoalEntryDto(Guid ScorerUserId, bool IsOwnGoal, Guid? AssistUserId);
