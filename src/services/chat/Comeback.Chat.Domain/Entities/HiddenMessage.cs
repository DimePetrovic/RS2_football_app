namespace Comeback.Chat.Domain.Entities;

/// <summary>
/// Per-user "delete message for me": a message the given user has hidden.
/// The message itself is retained on the server and other members still see it.
/// </summary>
public sealed class HiddenMessage
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid MessageId { get; private set; }
    public DateTime HiddenAt { get; private set; }

    private HiddenMessage() { }

    public HiddenMessage(Guid userId, Guid messageId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        MessageId = messageId;
        HiddenAt = DateTime.UtcNow;
    }
}
