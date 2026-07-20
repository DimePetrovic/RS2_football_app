namespace Comeback.Social.Application.Common.Interfaces;

using Comeback.Social.Application.DTOs;

public interface IFeedCache
{
    Task<List<PostResponse>?> GetAsync(Guid userId, CancellationToken ct = default);
    Task SetAsync(Guid userId, List<PostResponse> posts, CancellationToken ct = default);
    Task InvalidateAsync(Guid userId, CancellationToken ct = default);
}
