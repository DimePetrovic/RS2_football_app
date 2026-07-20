namespace Comeback.Social.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Social.Domain.Enums;

public sealed class Post : AggregateRoot<Guid>
{
    private readonly List<PostParticipant> _participants = [];
    private readonly List<PostComment> _comments = [];
    private readonly List<PostLike> _likes = [];

    public PostType Type { get; private set; }
    public Guid MatchId { get; private set; }
    public string MatchTitle { get; private set; } = string.Empty;
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }

    // PlayerWanted posts (public call for a missing player).
    public Guid OrganizerUserId { get; private set; }
    public string OrganizerDisplayName { get; private set; } = string.Empty;
    public string? Position { get; private set; }
    public string? Location { get; private set; }
    public DateTime? StartsAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<PostParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyList<PostComment> Comments => _comments.AsReadOnly();
    public IReadOnlyList<PostLike> Likes => _likes.AsReadOnly();

    private Post() { }

    private Post(Guid id, Guid matchId, string matchTitle, int homeScore, int awayScore) : base(id)
    {
        Type = PostType.MatchResult;
        MatchId = matchId;
        MatchTitle = matchTitle;
        HomeScore = homeScore;
        AwayScore = awayScore;
        CreatedAt = DateTime.UtcNow;
    }

    public static Post CreateMatchResultPost(
        Guid matchId, string matchTitle, int homeScore, int awayScore,
        IEnumerable<(Guid UserId, string DisplayName)> participants)
    {
        var post = new Post(Guid.NewGuid(), matchId, matchTitle, homeScore, awayScore);
        foreach (var (userId, displayName) in participants)
            post._participants.Add(PostParticipant.Create(post.Id, userId, displayName));
        return post;
    }

    private Post(Guid id) : base(id) => CreatedAt = DateTime.UtcNow;

    public static Post CreatePlayerWantedPost(
        Guid matchId, string matchTitle, Guid organizerUserId, string organizerDisplayName,
        string? position, string? location, DateTime startsAt)
        => new(Guid.NewGuid())
        {
            Type = PostType.PlayerWanted,
            MatchId = matchId,
            MatchTitle = matchTitle,
            OrganizerUserId = organizerUserId,
            OrganizerDisplayName = organizerDisplayName,
            Position = position,
            Location = location,
            StartsAt = startsAt,
        };

    public bool CanInteract => Type == PostType.MatchResult;

    public bool ToggleLike(Guid userId)
    {
        if (!CanInteract)
            throw new BusinessRuleException("This post does not support likes.", "post.likes_unsupported");

        var existing = _likes.FirstOrDefault(l => l.UserId == userId);
        if (existing is not null)
        {
            _likes.Remove(existing);
            return false;
        }

        _likes.Add(PostLike.Create(Id, userId));
        return true;
    }

    public PostComment AddComment(Guid authorUserId, string authorDisplayName, string content)
    {
        if (!CanInteract)
            throw new BusinessRuleException("This post does not support comments.", "post.comments_unsupported");
        if (string.IsNullOrWhiteSpace(content))
            throw new BusinessRuleException("Comment cannot be empty.", "comment.empty");
        if (content.Length > 1000)
            throw new BusinessRuleException("Comment is too long.", "comment.too_long");

        var comment = PostComment.Create(Id, authorUserId, authorDisplayName, content.Trim());
        _comments.Add(comment);
        return comment;
    }
}
