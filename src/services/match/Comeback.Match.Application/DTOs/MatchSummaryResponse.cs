namespace Comeback.Match.Application.DTOs;

public sealed record MatchSummaryResponse(
    Guid Id,
    string Title,
    string Type,
    string Status,
    Guid OrganizerUserId,
    string? Location,
    DateTime StartsAt,
    int? DurationMinutes,
    int PlayersPerTeam,
    int AcceptedCount,
    DateTime CreatedAt);
