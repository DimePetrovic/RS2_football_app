namespace Comeback.Chat.Domain.Entities;

using Comeback.Chat.Domain.Enums;

public sealed class Conversation
{
    private readonly List<ConversationMember> _members = [];
    private readonly List<Message> _messages = [];

    public Guid Id { get; private set; }
    public ConversationType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Group conversations are backed by a real player group (source of truth for membership is the Profile service).
    public Guid? GroupId { get; private set; }
    public string? Title { get; private set; }
    public string? GroupAvatarUrl { get; private set; }

    public IReadOnlyList<ConversationMember> Members => _members.AsReadOnly();
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    private Conversation() { }

    public static Conversation CreateDirect(Guid userId1, string displayName1, Guid userId2, string displayName2)
    {
        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Direct,
            CreatedAt = DateTime.UtcNow,
        };
        conv._members.Add(new ConversationMember(conv.Id, userId1, displayName1));
        conv._members.Add(new ConversationMember(conv.Id, userId2, displayName2));
        return conv;
    }

    public static Conversation CreateGroup(Guid groupId, string title, string? avatarUrl)
        => new()
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Group,
            CreatedAt = DateTime.UtcNow,
            GroupId = groupId,
            Title = title,
            GroupAvatarUrl = avatarUrl,
        };

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public void UpdateGroupMeta(string title, string? avatarUrl)
    {
        Title = title;
        GroupAvatarUrl = avatarUrl;
    }

    /// <summary>Materializes a member row on demand (group members appear lazily as they interact / receive messages).</summary>
    public ConversationMember EnsureMember(Guid userId, string displayName)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            member = new ConversationMember(Id, userId, displayName);
            _members.Add(member);
        }
        return member;
    }
}
