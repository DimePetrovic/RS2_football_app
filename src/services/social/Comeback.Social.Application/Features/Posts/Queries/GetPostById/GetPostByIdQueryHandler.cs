namespace Comeback.Social.Application.Features.Posts.Queries.GetPostById;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Social.Application.Common;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using MediatR;

public sealed class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostResponse>
{
    private readonly IPostRepository _posts;
    private readonly PostEnricher _enricher;

    public GetPostByIdQueryHandler(IPostRepository posts, PostEnricher enricher)
    {
        _posts = posts;
        _enricher = enricher;
    }

    public async Task<PostResponse> Handle(GetPostByIdQuery query, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(query.PostId, ct)
            ?? throw new NotFoundException("Post not found.", "post.not_found");

        return await _enricher.EnrichAsync(post, query.CurrentUserId, ct);
    }
}
