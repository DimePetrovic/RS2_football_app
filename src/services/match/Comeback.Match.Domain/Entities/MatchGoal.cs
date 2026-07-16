namespace Comeback.Match.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Match.Domain.Enums;

public sealed class MatchGoal : Entity<Guid>
{
    public Guid MatchId { get; private set; }
    public Guid ScorerUserId { get; private set; }
    public string ScorerDisplayName { get; private set; } = string.Empty;
    public MatchTeam ScoringTeam { get; private set; }
    public bool IsOwnGoal { get; private set; }
    public Guid? AssistUserId { get; private set; }
    public string? AssistDisplayName { get; private set; }

    private MatchGoal() { }

    private MatchGoal(
        Guid id, Guid matchId, Guid scorerUserId, string scorerDisplayName,
        MatchTeam scoringTeam, bool isOwnGoal, Guid? assistUserId, string? assistDisplayName) : base(id)
    {
        MatchId = matchId;
        ScorerUserId = scorerUserId;
        ScorerDisplayName = scorerDisplayName;
        ScoringTeam = scoringTeam;
        IsOwnGoal = isOwnGoal;
        AssistUserId = assistUserId;
        AssistDisplayName = assistDisplayName;
    }

    internal static MatchGoal Create(
        Guid matchId, Guid scorerUserId, string scorerDisplayName,
        MatchTeam scoringTeam, bool isOwnGoal, Guid? assistUserId, string? assistDisplayName)
        => new(Guid.NewGuid(), matchId, scorerUserId, scorerDisplayName, scoringTeam, isOwnGoal, assistUserId, assistDisplayName);
}
