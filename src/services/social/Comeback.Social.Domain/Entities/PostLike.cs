namespace Comeback.Social.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class PostLike : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PostLike() { }

    private PostLike(Guid id, Guid postId, Guid userId) : base(id)
    {
        PostId = postId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    internal static PostLike Create(Guid postId, Guid userId) => new(Guid.NewGuid(), postId, userId);
}
