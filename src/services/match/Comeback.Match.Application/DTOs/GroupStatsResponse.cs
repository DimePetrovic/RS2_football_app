namespace Comeback.Match.Application.DTOs;

public sealed record GroupOpponentStat(
    Guid GroupId,
    string GroupName,
    int Played,
    int Wins,
    int Draws,
    int Losses);

/// <summary>Group statistics — only group-vs-group matches with an entered result are counted.</summary>
public sealed record GroupStatsResponse(
    int PlayedCount,
    int Wins,
    int Draws,
    int Losses,
    IReadOnlyList<GroupOpponentStat> Opponents);
