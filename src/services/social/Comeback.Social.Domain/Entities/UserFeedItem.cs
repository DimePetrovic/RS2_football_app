namespace Comeback.Social.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class UserFeedItem : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid PostId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserFeedItem() { }

    private UserFeedItem(Guid id, Guid userId, Guid postId, DateTime createdAt) : base(id)
    {
        UserId = userId;
        PostId = postId;
        CreatedAt = createdAt;
    }

    public static UserFeedItem Create(Guid userId, Guid postId, DateTime createdAt)
        => new(Guid.NewGuid(), userId, postId, createdAt);
}
