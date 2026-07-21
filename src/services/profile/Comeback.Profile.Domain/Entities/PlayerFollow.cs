namespace Comeback.Profile.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class PlayerFollow : Entity<Guid>
{
    public Guid FollowerUserId { get; private set; }
    public Guid FollowedUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PlayerFollow() { }

    private PlayerFollow(Guid id, Guid followerUserId, Guid followedUserId) : base(id)
    {
        FollowerUserId = followerUserId;
        FollowedUserId = followedUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public static PlayerFollow Create(Guid followerUserId, Guid followedUserId)
        => new(Guid.NewGuid(), followerUserId, followedUserId);
}
