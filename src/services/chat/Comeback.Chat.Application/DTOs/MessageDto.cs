namespace Comeback.Chat.Application.DTOs;

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string? SenderUsername,
    string? SenderAvatarUrl,
    string? SenderNationality,
    string Content,
    DateTime SentAt,
    bool IsRead
);
