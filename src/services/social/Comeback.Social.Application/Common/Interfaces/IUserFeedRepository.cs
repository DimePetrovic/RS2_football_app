namespace Comeback.Social.Application.Common.Interfaces;

using Comeback.Social.Domain.Entities;

public interface IUserFeedRepository
{
    Task<List<UserFeedItem>> GetFeedAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<List<Guid>> GetExistingUserIdsWithPostAsync(Guid postId, IEnumerable<Guid> userIds, CancellationToken ct = default);
    Task<List<Guid>> GetUserIdsForPostAsync(Guid postId, CancellationToken ct = default);
    void AddRange(IEnumerable<UserFeedItem> items);
}
