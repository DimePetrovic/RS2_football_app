namespace Comeback.Social.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class PostComment : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string AuthorDisplayName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private PostComment() { }

    private PostComment(Guid id, Guid postId, Guid authorUserId, string authorDisplayName, string content) : base(id)
    {
        PostId = postId;
        AuthorUserId = authorUserId;
        AuthorDisplayName = authorDisplayName;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }

    internal static PostComment Create(Guid postId, Guid authorUserId, string authorDisplayName, string content)
        => new(Guid.NewGuid(), postId, authorUserId, authorDisplayName, content);
}
