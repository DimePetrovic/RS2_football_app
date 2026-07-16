namespace Comeback.Match.Application.DTOs;

public sealed record ParticipantResponse(
    Guid Id,
    Guid UserId,
    string DisplayName,
    bool IsOrganizer,
    bool IsCaptain,
    string Team,
    string Status,
    DateTime InvitedAt,
    DateTime? RespondedAt,
    bool IsBench,
    bool IsGuest,
    string? Username,
    string? AvatarUrl,
    string? Nationality);
