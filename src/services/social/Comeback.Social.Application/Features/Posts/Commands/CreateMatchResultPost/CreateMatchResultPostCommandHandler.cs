namespace Comeback.Social.Application.Features.Posts.Commands.CreateMatchResultPost;

using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Domain.Entities;
using Comeback.Social.Domain.Enums;
using MediatR;

public sealed class CreateMatchResultPostCommandHandler : IRequestHandler<CreateMatchResultPostCommand>
{
    private readonly IPostRepository _posts;
    private readonly IUserFeedRepository _feed;
    private readonly IProfileFollowersClient _followersClient;
    private readonly IFeedCache _cache;
    private readonly ISocialUnitOfWork _unitOfWork;

    public CreateMatchResultPostCommandHandler(
        IPostRepository posts,
        IUserFeedRepository feed,
        IProfileFollowersClient followersClient,
        IFeedCache cache,
        ISocialUnitOfWork unitOfWork)
    {
        _posts = posts;
        _feed = feed;
        _followersClient = followersClient;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateMatchResultPostCommand command, CancellationToken ct)
    {
        // Idempotency: a retried/duplicate event must not create a second post for the same match.
        var existing = await _posts.GetByMatchIdAndTypeAsync(command.MatchId, PostType.MatchResult, ct);
        if (existing is not null) return;

        var post = Post.CreateMatchResultPost(
            command.MatchId,
            command.MatchTitle,
            command.HomeScore,
            command.AwayScore,
            command.Participants.Select(p => (p.UserId, p.DisplayName)));

        _posts.Add(post);

        var participantIds = command.Participants.Select(p => p.UserId).ToList();
        var followerIds = await _followersClient.GetFollowersForAnyAsync(participantIds, ct);

        // Relevant via own participation OR via following any participant — deduplicated into a single feed entry per user.
        var targetUserIds = new HashSet<Guid>(participantIds);
        targetUserIds.UnionWith(followerIds);

        var feedItems = targetUserIds
            .Select(userId => UserFeedItem.Create(userId, post.Id, post.CreatedAt))
            .ToList();
        _feed.AddRange(feedItems);

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var userId in targetUserIds)
            await _cache.InvalidateAsync(userId, ct);
    }
}
