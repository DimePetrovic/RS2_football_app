namespace Comeback.Chat.Domain.Entities;

public sealed class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string SenderDisplayName { get; private set; } = string.Empty;
    public string EncryptedContent { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }

    private Message() { }

    public static Message Create(Guid conversationId, Guid senderUserId, string senderDisplayName, string encryptedContent)
        => new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            SenderDisplayName = senderDisplayName,
            EncryptedContent = encryptedContent,
            SentAt = DateTime.UtcNow,
        };
}
