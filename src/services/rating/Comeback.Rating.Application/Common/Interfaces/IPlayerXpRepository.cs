namespace Comeback.Rating.Application.Common.Interfaces;

using Comeback.Rating.Domain.Entities;

public interface IPlayerXpRepository
{
    Task<PlayerXp?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(PlayerXp playerXp);
    void Update(PlayerXp playerXp);

    /// <summary>True if this match's XP was already awarded to this player (dedup guard against redelivery).</summary>
    Task<bool> HasAwardedMatchXpAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Records that this match's XP has now been awarded to this player.</summary>
    void MarkMatchXpAwarded(AwardedMatchXp record);
}
