namespace Comeback.Social.Application.Common.Interfaces;

using Comeback.Social.Domain.Entities;
using Comeback.Social.Domain.Enums;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Post?> GetByMatchIdAsync(Guid matchId, CancellationToken ct = default);
    Task<Post?> GetByMatchIdAndTypeAsync(Guid matchId, PostType type, CancellationToken ct = default);
    Task<List<Post>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<List<PostComment>> GetCommentsAsync(Guid postId, CancellationToken ct = default);
    void Add(Post post);
}
