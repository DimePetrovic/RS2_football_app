namespace Comeback.Match.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class MatchPlayerReview : Entity<Guid>
{
    public Guid MatchId { get; private set; }
    public Guid ReviewerParticipantId { get; private set; }
    public Guid ReviewedParticipantId { get; private set; }
    public decimal OverallRating { get; private set; }
    public decimal? GoalkeepingRating { get; private set; }
    public decimal? DefenseRating { get; private set; }
    public decimal? AttackRating { get; private set; }
    public decimal? EffortRating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private MatchPlayerReview() { }

    private MatchPlayerReview(
        Guid id, Guid matchId,
        Guid reviewerParticipantId, Guid reviewedParticipantId,
        decimal overallRating,
        decimal? goalkeepingRating, decimal? defenseRating,
        decimal? attackRating, decimal? effortRating,
        string? comment) : base(id)
    {
        MatchId = matchId;
        ReviewerParticipantId = reviewerParticipantId;
        ReviewedParticipantId = reviewedParticipantId;
        OverallRating = overallRating;
        GoalkeepingRating = goalkeepingRating;
        DefenseRating = defenseRating;
        AttackRating = attackRating;
        EffortRating = effortRating;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    public static MatchPlayerReview Create(
        Guid matchId,
        Guid reviewerParticipantId,
        Guid reviewedParticipantId,
        decimal overallRating,
        decimal? goalkeepingRating,
        decimal? defenseRating,
        decimal? attackRating,
        decimal? effortRating,
        string? comment)
        => new(Guid.NewGuid(), matchId, reviewerParticipantId, reviewedParticipantId,
               overallRating, goalkeepingRating, defenseRating, attackRating, effortRating, comment);

    public void Update(
        decimal overallRating,
        decimal? goalkeepingRating,
        decimal? defenseRating,
        decimal? attackRating,
        decimal? effortRating,
        string? comment)
    {
        OverallRating = overallRating;
        GoalkeepingRating = goalkeepingRating;
        DefenseRating = defenseRating;
        AttackRating = attackRating;
        EffortRating = effortRating;
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }
}
