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

        var result = new List<PostResponse>();
        foreach (var feedItem in feedItems)
        {
            if (!postsById.TryGetValue(feedItem.PostId, out var post)) continue;
            result.Add(await _enricher.EnrichAsync(post, query.UserId, ct));
        }

        if (query.Page == 0)
            await _cache.SetAsync(query.UserId, result, ct);

        return result;
    }
}
