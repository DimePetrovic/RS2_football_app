namespace Comeback.Rating.Application.Common.Interfaces;

using Comeback.Rating.Domain.Entities;

public interface IPlayerXpRepository
{
    Task<PlayerXp?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(PlayerXp playerXp);
    void Update(PlayerXp playerXp);
}
