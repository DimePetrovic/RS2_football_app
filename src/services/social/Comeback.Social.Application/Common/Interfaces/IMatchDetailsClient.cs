namespace Comeback.Social.Application.Common.Interfaces;

public sealed record MatchParticipantInfo(
    Guid ParticipantId, Guid UserId, string DisplayName, string Team, bool IsCaptain, string Status);

public sealed record MatchGoalInfo(Guid ScorerUserId, string ScoringTeam, bool IsOwnGoal, Guid? AssistUserId);

public sealed record MatchDetailsInfo(
    IReadOnlyList<MatchParticipantInfo> Participants,
    IReadOnlyList<MatchGoalInfo> Goals,
    string? Location,
    DateTime StartsAt,
    string? GroupName,
    string? OpponentGroupName);

public sealed record MatchReviewInfo(
    Guid ReviewerParticipantId,
    Guid ReviewedParticipantId,
    decimal OverallRating,
    decimal? GoalkeepingRating,
    decimal? DefenseRating,
    decimal? AttackRating,
    decimal? EffortRating,
    string? Comment);

public interface IMatchDetailsClient
{
    Task<MatchDetailsInfo?> GetMatchDetailsAsync(Guid matchId, CancellationToken ct = default);
    Task<List<MatchReviewInfo>> GetReviewsAsync(Guid matchId, CancellationToken ct = default);
}
