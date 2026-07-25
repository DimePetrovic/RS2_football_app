namespace Comeback.Social.Application.Features.Feed.Queries.GetFeed;

using Comeback.Social.Application.Common;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using MediatR;

public sealed class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, List<PostResponse>>
{
    private readonly IUserFeedRepository _feedRepository;
    private readonly IPostRepository _posts;
    private readonly IFeedCache _cache;
    private readonly PostEnricher _enricher;

    public GetFeedQueryHandler(
        IUserFeedRepository feedRepository, IPostRepository posts, IFeedCache cache, PostEnricher enricher)
    {
        _feedRepository = feedRepository;
        _posts = posts;
        _cache = cache;
        _enricher = enricher;
    }

    public async Task<List<PostResponse>> Handle(GetFeedQuery query, CancellationToken ct)
    {
        if (query.Page == 0)
        {
            var cached = await _cache.GetAsync(query.UserId, ct);
            if (cached is not null) return cached;
        }

        var feedItems = await _feedRepository.GetFeedAsync(
            query.UserId, query.Page * query.PageSize, query.PageSize, ct);

        if (feedItems.Count == 0) return [];

        var posts = await _posts.GetByIdsAsync(feedItems.Select(f => f.PostId), ct);
        var postsById = posts.ToDictionary(p => p.Id);

        // Enrich all posts concurrently; each EnrichAsync fans out to independent Match/Profile HTTP calls.
        // Task.WhenAll preserves input order, so the feed keeps its original ordering.
        var enrichTasks = feedItems
            .Where(f => postsById.ContainsKey(f.PostId))
            .Select(f => _enricher.EnrichAsync(postsById[f.PostId], query.UserId, ct));
        var result = (await Task.WhenAll(enrichTasks)).ToList();

        if (query.Page == 0)
            await _cache.SetAsync(query.UserId, result, ct);

        return result;
    }
}
