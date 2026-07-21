namespace Comeback.Social.Application.Features.Posts.Queries.GetComments;

using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using MediatR;

public sealed class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, List<CommentResponse>>
{
    private readonly IPostRepository _posts;

    public GetCommentsQueryHandler(IPostRepository posts) => _posts = posts;

    public async Task<List<CommentResponse>> Handle(GetCommentsQuery query, CancellationToken ct)
    {
        var comments = await _posts.GetCommentsAsync(query.PostId, ct);
        return comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse(c.Id, c.AuthorUserId, c.AuthorDisplayName, c.Content, c.CreatedAt))
            .ToList();
    }
}
