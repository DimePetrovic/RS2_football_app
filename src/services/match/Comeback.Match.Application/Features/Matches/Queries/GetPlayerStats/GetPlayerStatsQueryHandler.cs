namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayerStats;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class GetPlayerStatsQueryHandler : IRequestHandler<GetPlayerStatsQuery, PlayerStatsResponse>
{
    private readonly IMatchRepository _matches;
    private readonly IPlayerInfoClient _playerInfo;

    public GetPlayerStatsQueryHandler(IMatchRepository matches, IPlayerInfoClient playerInfo)
    {
        _matches = matches;
        _playerInfo = playerInfo;
    }

    public async Task<PlayerStatsResponse> Handle(GetPlayerStatsQuery query, CancellationToken ct)
    {
        var matches = await _matches.GetByUserIdAsync(query.UserId, ct);

        var organized = matches.Where(m => m.OrganizerUserId == query.UserId).ToList();
        var organizedWithResult = organized.Count(HasResult);

        // Played match = accepted participant with an assigned team and an entered result.
        var played = matches
            .Select(m => (Match: m, Me: m.Participants.FirstOrDefault(p =>
                p.UserId == query.UserId &&
                p.Status == MatchParticipantStatus.Accepted &&
                p.Team != MatchTeam.None)))
            .Where(x => x.Me is not null && HasResult(x.Match))
            .Select(x => (x.Match, Me: x.Me!,
                Outcome: MatchScore.OutcomeFor(x.Match.HomeScore!.Value, x.Match.AwayScore!.Value, x.Me!.Team).ToString()))
            .OrderBy(x => x.Match.StartsAt)
            .ToList();

        var timeline = played
            .Select(x => new PlayerStatsTimelineItem(x.Match.Id, x.Match.StartsAt, x.Outcome))
            .ToList();

        var topBeaten = TopOpponents(played, "Win");
        var topLostTo = TopOpponents(played, "Loss");

        // Players are always shown with avatar and username — enrichment from the Profile service.
        var infos = (await _playerInfo.GetPlayerInfosAsync(
                topBeaten.Concat(topLostTo).Select(o => o.UserId), ct))
            .ToDictionary(i => i.UserId);
        topBeaten = topBeaten.Select(o => Enrich(o, infos)).ToList();
        topLostTo = topLostTo.Select(o => Enrich(o, infos)).ToList();

        // Goals/assists are counted on played matches; own goals do not count as goals.
        var goals = played.Sum(x => x.Match.Goals.Count(
            g => g.ScorerUserId == query.UserId && !g.IsOwnGoal));
        var assists = played.Sum(x => x.Match.Goals.Count(
            g => g.AssistUserId == query.UserId));

        var groupsPlayedWith = played
            .Select(x => GroupFor(x.Match, x.Me.Team))
            .Where(g => g is not null)
            .GroupBy(g => g!.Value.Id)
            .Select(g => new GroupPlayStat(g.Key, g.Last()!.Value.Name, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        return new PlayerStatsResponse(
            organized.Count,
            organizedWithResult,
            played.Count,
            played.Count(x => x.Outcome == "Win"),
            played.Count(x => x.Outcome == "Draw"),
            played.Count(x => x.Outcome == "Loss"),
            goals,
            assists,
            timeline,
            topBeaten,
            topLostTo,
            groupsPlayedWith);
    }

    private static PlayerOpponentStat Enrich(
        PlayerOpponentStat stat, IReadOnlyDictionary<Guid, PlayerInfo> infos)
        => infos.TryGetValue(stat.UserId, out var info)
            ? stat with
            {
                Username = info.Username,
                AvatarUrl = info.AvatarUrl,
                Nationality = info.Nationality,
                DisplayName = string.IsNullOrWhiteSpace(info.DisplayName) ? stat.DisplayName : info.DisplayName!,
            }
            : stat;

    private static bool HasResult(Match m) => m.HomeScore.HasValue && m.AwayScore.HasValue;

    private static List<PlayerOpponentStat> TopOpponents(
        IReadOnlyList<(Match Match, MatchParticipant Me, string Outcome)> played,
        string outcome)
        => played
            .Where(x => x.Outcome == outcome)
            .SelectMany(x => x.Match.Participants.Where(p =>
                p.Status == MatchParticipantStatus.Accepted &&
                p.Team != MatchTeam.None &&
                p.Team != x.Me.Team &&
                !p.IsGuest))
            .GroupBy(p => p.UserId)
            .Select(g => new PlayerOpponentStat(g.Key, g.Last().DisplayName, g.Last().DisplayName, null, null, g.Count()))
            .OrderByDescending(o => o.Count)
            .ThenBy(o => o.DisplayName)
            .Take(3)
            .ToList();

    private static (Guid Id, string Name)? GroupFor(Match m, MatchTeam myTeam)
    {
        if (m.Type == MatchType.GroupMatch && m.GroupId.HasValue)
            return (m.GroupId.Value, m.GroupName ?? string.Empty);

        if (m.Type == MatchType.GroupVsGroup)
        {
            if (myTeam == MatchTeam.Home && m.GroupId.HasValue)
                return (m.GroupId.Value, m.GroupName ?? string.Empty);
            if (myTeam == MatchTeam.Away && m.OpponentGroupId.HasValue)
                return (m.OpponentGroupId.Value, m.OpponentGroupName ?? string.Empty);
        }

        return null;
    }
}
