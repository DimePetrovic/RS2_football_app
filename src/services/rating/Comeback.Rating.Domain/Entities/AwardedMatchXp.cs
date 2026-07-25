namespace Comeback.Rating.Domain.Entities;

/// <summary>
/// Dedup record proving that a given (match, player) pair already had its match XP awarded.
/// The composite (MatchId, UserId) primary key makes XP awarding idempotent: a redelivered or
/// retried MatchResultSubmitted event cannot add the same match's XP to a player twice.
/// </summary>
public sealed class AwardedMatchXp
{
    public Guid MatchId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AwardedAt { get; private set; }

    private AwardedMatchXp() { }

    public AwardedMatchXp(Guid matchId, Guid userId)
    {
        MatchId = matchId;
        UserId = userId;
        AwardedAt = DateTime.UtcNow;
    }
}
