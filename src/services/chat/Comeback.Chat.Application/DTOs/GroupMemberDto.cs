namespace Comeback.Chat.Application.DTOs;

public sealed record GroupMemberDto(
    Guid UserId,
    string DisplayName,
    string? Username,
    string? AvatarUrl,
    string? Nationality
);
