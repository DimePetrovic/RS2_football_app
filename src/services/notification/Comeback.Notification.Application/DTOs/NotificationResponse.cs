namespace Comeback.Notification.Application.DTOs;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string? Payload,
    string? LegacyTitle,
    string? LegacyBody,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);
