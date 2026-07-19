namespace Comeback.Chat.Domain.Entities;

public sealed class ConversationMember
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    // Per-user "delete conversation for me": messages up to this moment are hidden from this member.
    public DateTime? ClearedAt { get; private set; }

    public void MarkAsRead() => LastReadAt = DateTime.UtcNow;

    public void ClearHistory() => ClearedAt = DateTime.UtcNow;

    private ConversationMember() { }

    public ConversationMember(Guid conversationId, Guid userId, string displayName)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        UserId = userId;
        DisplayName = displayName;
        JoinedAt = DateTime.UtcNow;
    }
}
