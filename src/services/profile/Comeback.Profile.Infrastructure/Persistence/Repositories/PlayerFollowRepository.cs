namespace Comeback.Profile.Infrastructure.Persistence.Repositories;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class PlayerFollowRepository : IPlayerFollowRepository
{
    private readonly ProfileDbContext _context;

    public PlayerFollowRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public Task<PlayerFollow?> GetAsync(Guid followerUserId, Guid followedUserId, CancellationToken ct = default)
        => _context.PlayerFollows.FirstOrDefaultAsync(
            f => f.FollowerUserId == followerUserId && f.FollowedUserId == followedUserId, ct);

    public Task<List<Guid>> GetFollowingIdsAsync(Guid followerUserId, CancellationToken ct = default)
        => _context.PlayerFollows
            .Where(f => f.FollowerUserId == followerUserId)
            .Select(f => f.FollowedUserId)
            .ToListAsync(ct);

    public Task<List<Guid>> GetFollowerIdsAsync(Guid followedUserId, CancellationToken ct = default)
        => _context.PlayerFollows
            .Where(f => f.FollowedUserId == followedUserId)
            .Select(f => f.FollowerUserId)
            .ToListAsync(ct);

    public Task<List<Guid>> GetFollowerIdsForAnyAsync(IEnumerable<Guid> followedUserIds, CancellationToken ct = default)
    {
        var ids = followedUserIds.ToList();
        return _context.PlayerFollows
            .Where(f => ids.Contains(f.FollowedUserId))
            .Select(f => f.FollowerUserId)
            .Distinct()
            .ToListAsync(ct);
    }

    public Task<int> CountFollowersAsync(Guid followedUserId, CancellationToken ct = default)
        => _context.PlayerFollows.CountAsync(f => f.FollowedUserId == followedUserId, ct);

    public Task<int> CountFollowingAsync(Guid followerUserId, CancellationToken ct = default)
        => _context.PlayerFollows.CountAsync(f => f.FollowerUserId == followerUserId, ct);

    public void Add(PlayerFollow follow) => _context.PlayerFollows.Add(follow);

    public void Remove(PlayerFollow follow) => _context.PlayerFollows.Remove(follow);
}
