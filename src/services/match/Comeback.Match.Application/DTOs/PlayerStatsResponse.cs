namespace Comeback.Match.Application.DTOs;

/// <summary>A single played match in the timeline — the frontend groups by period.</summary>
public sealed record PlayerStatsTimelineItem(Guid MatchId, DateTime StartsAt, string Outcome);

public sealed record PlayerOpponentStat(
    Guid UserId,
    string DisplayName,
    string Username,
    string? Nationality,
    string? AvatarUrl,
    int Count);

public sealed record GroupPlayStat(Guid GroupId, string GroupName, int Count);

public sealed record PlayerStatsResponse(
    int OrganizedCount,
    int OrganizedWithResult,
    int PlayedCount,
    int Wins,
    int Draws,
    int Losses,
    int Goals,
    int Assists,
    IReadOnlyList<PlayerStatsTimelineItem> Timeline,
    IReadOnlyList<PlayerOpponentStat> TopBeaten,
    IReadOnlyList<PlayerOpponentStat> TopLostTo,
    IReadOnlyList<GroupPlayStat> GroupsPlayedWith);
