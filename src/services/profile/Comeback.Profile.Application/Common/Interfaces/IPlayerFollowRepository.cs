namespace Comeback.Profile.Application.Common.Interfaces;

using Comeback.Profile.Domain.Entities;

public interface IPlayerFollowRepository
{
    Task<PlayerFollow?> GetAsync(Guid followerUserId, Guid followedUserId, CancellationToken ct = default);
    Task<List<Guid>> GetFollowingIdsAsync(Guid followerUserId, CancellationToken ct = default);
    Task<List<Guid>> GetFollowerIdsAsync(Guid followedUserId, CancellationToken ct = default);
    Task<List<Guid>> GetFollowerIdsForAnyAsync(IEnumerable<Guid> followedUserIds, CancellationToken ct = default);
    Task<int> CountFollowersAsync(Guid followedUserId, CancellationToken ct = default);
    Task<int> CountFollowingAsync(Guid followerUserId, CancellationToken ct = default);
    void Add(PlayerFollow follow);
    void Remove(PlayerFollow follow);
}
