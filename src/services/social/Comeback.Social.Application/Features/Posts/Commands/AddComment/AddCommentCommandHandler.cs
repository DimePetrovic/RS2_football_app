namespace Comeback.Social.Application.Features.Posts.Commands.AddComment;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using MediatR;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentResponse>
{
    private readonly IPostRepository _posts;
    private readonly IUserFeedRepository _feed;
    private readonly IFeedCache _cache;
    private readonly ISocialUnitOfWork _unitOfWork;

    public AddCommentCommandHandler(
        IPostRepository posts, IUserFeedRepository feed, IFeedCache cache, ISocialUnitOfWork unitOfWork)
    {
        _posts = posts;
        _feed = feed;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommentResponse> Handle(AddCommentCommand command, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(command.PostId, ct)
            ?? throw new NotFoundException("Post not found.", "post.not_found");

        var comment = post.AddComment(command.AuthorUserId, command.AuthorDisplayName, command.Content);
        await _unitOfWork.SaveChangesAsync(ct);

        var viewerIds = await _feed.GetUserIdsForPostAsync(command.PostId, ct);
        foreach (var userId in viewerIds)
            await _cache.InvalidateAsync(userId, ct);

        return new CommentResponse(
            comment.Id, comment.AuthorUserId, comment.AuthorDisplayName, comment.Content, comment.CreatedAt);
    }
}
