namespace Comeback.Social.Application.Features.Posts.Commands.CreatePlayerWantedPost;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Domain.Entities;
using Comeback.Social.Domain.Enums;
using MediatR;

public sealed class CreatePlayerWantedPostCommandHandler : IRequestHandler<CreatePlayerWantedPostCommand>
{
    private readonly IPostRepository _posts;
    private readonly IUserFeedRepository _feed;
    private readonly IProfileFollowersClient _profile;
    private readonly IFeedCache _cache;
    private readonly ISocialUnitOfWork _unitOfWork;

    public CreatePlayerWantedPostCommandHandler(
        IPostRepository posts, IUserFeedRepository feed, IProfileFollowersClient profile,
        IFeedCache cache, ISocialUnitOfWork unitOfWork)
    {
        _posts = posts;
        _feed = feed;
        _profile = profile;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreatePlayerWantedPostCommand command, CancellationToken ct)
    {
        // One public call per match (idempotent for retried events).
        var existing = await _posts.GetByMatchIdAndTypeAsync(command.MatchId, PostType.PlayerWanted, ct);
        if (existing is not null) return;

        var post = Post.CreatePlayerWantedPost(
            command.MatchId, command.MatchTitle, command.OrganizerUserId, command.OrganizerDisplayName,
            command.Position, command.Location, command.StartsAt);
        _posts.Add(post);

        // A public call appears on everyone's feed.
        var allUserIds = await _profile.GetAllUserIdsAsync(ct);
        var feedItems = allUserIds
            .Select(userId => UserFeedItem.Create(userId, post.Id, post.CreatedAt))
            .ToList();
        _feed.AddRange(feedItems);

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var userId in allUserIds)
            await _cache.InvalidateAsync(userId, ct);
    }
}
