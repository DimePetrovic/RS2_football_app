namespace Comeback.Social.Application.Common.Interfaces;

public interface IProfileFollowersClient
{
    Task<List<Guid>> GetFollowersForAnyAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);

    /// <summary>All registered user ids — used to fan a public post out to everyone.</summary>
    Task<List<Guid>> GetAllUserIdsAsync(CancellationToken ct = default);
}
