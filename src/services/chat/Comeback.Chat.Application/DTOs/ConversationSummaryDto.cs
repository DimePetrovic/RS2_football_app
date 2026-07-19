namespace Comeback.Chat.Application.DTOs;

using Comeback.Chat.Domain.Enums;

public sealed record ConversationSummaryDto(
    Guid ConversationId,
    ConversationType Type,
    Guid? OtherUserId,
    string? OtherUserDisplayName,
    Guid? GroupId,
    string? Title,
    string? AvatarUrl,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    bool HasUnread
);
