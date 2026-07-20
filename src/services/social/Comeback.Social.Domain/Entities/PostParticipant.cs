namespace Comeback.Social.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class PostParticipant : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    private PostParticipant() { }

    private PostParticipant(Guid id, Guid postId, Guid userId, string displayName) : base(id)
    {
        PostId = postId;
        UserId = userId;
        DisplayName = displayName;
    }

    internal static PostParticipant Create(Guid postId, Guid userId, string displayName)
        => new(Guid.NewGuid(), postId, userId, displayName);
}
