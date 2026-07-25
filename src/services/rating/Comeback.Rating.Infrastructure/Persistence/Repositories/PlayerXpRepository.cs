namespace Comeback.Rating.Infrastructure.Persistence.Repositories;

using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Domain.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class PlayerXpRepository : IPlayerXpRepository
{
    private readonly RatingDbContext _context;

    public PlayerXpRepository(RatingDbContext context)
    {
        _context = context;
    }

    public Task<PlayerXp?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.PlayerXps.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(PlayerXp playerXp) => _context.PlayerXps.Add(playerXp);

    public void Update(PlayerXp playerXp) => _context.PlayerXps.Update(playerXp);

    public Task<bool> HasAwardedMatchXpAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
        => _context.AwardedMatchXps.AnyAsync(a => a.MatchId == matchId && a.UserId == userId, cancellationToken);

    public void MarkMatchXpAwarded(AwardedMatchXp record) => _context.AwardedMatchXps.Add(record);
}
