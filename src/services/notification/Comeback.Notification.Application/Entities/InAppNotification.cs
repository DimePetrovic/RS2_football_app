namespace Comeback.Notification.Application.Entities;

public sealed class InAppNotification
{
    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Payload { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private InAppNotification() { }

    public InAppNotification(Guid recipientUserId, string type, string? payload = null)
    {
        Id = Guid.NewGuid();
        RecipientUserId = recipientUserId;
        Type = type;
        // Localized title/body are rendered by the client from Type + Payload; columns kept for compatibility.
        Title = string.Empty;
        Body = string.Empty;
        Payload = payload;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
