namespace Comeback.Match.Domain.Entities;

public sealed record GoalEntry(Guid ScorerUserId, bool IsOwnGoal, Guid? AssistUserId);
