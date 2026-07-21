namespace Comeback.Social.Application.Features.Posts.Commands.ToggleLike;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Social.Application.Common.Interfaces;
using MediatR;

public sealed class ToggleLikeCommandHandler : IRequestHandler<ToggleLikeCommand, bool>
{
    private readonly IPostRepository _posts;
    private readonly IUserFeedRepository _feed;
    private readonly IFeedCache _cache;
    private readonly ISocialUnitOfWork _unitOfWork;

    public ToggleLikeCommandHandler(
        IPostRepository posts, IUserFeedRepository feed, IFeedCache cache, ISocialUnitOfWork unitOfWork)
    {
        _posts = posts;
        _feed = feed;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ToggleLikeCommand command, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(command.PostId, ct)
            ?? throw new NotFoundException("Post not found.", "post.not_found");

        var isLikedNow = post.ToggleLike(command.UserId);
        await _unitOfWork.SaveChangesAsync(ct);

        var viewerIds = await _feed.GetUserIdsForPostAsync(command.PostId, ct);
        foreach (var userId in viewerIds)
            await _cache.InvalidateAsync(userId, ct);

        return isLikedNow;
    }
}
