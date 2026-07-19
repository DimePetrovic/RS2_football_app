namespace Comeback.Match.Application.Features.Matches.Queries.GetGroupStats;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class GetGroupStatsQueryHandler : IRequestHandler<GetGroupStatsQuery, GroupStatsResponse>
{
    private readonly IMatchRepository _matches;

    public GetGroupStatsQueryHandler(IMatchRepository matches)
        => _matches = matches;

    public async Task<GroupStatsResponse> Handle(GetGroupStatsQuery query, CancellationToken ct)
    {
        var matches = await _matches.GetByGroupIdAsync(query.GroupId, ct);

        // Group statistics only make sense for group-vs-group matches with a result.
        var played = matches
            .Where(m => m.Type == MatchType.GroupVsGroup
                     && m.HomeScore.HasValue && m.AwayScore.HasValue
                     && (m.GroupId == query.GroupId || m.OpponentGroupId == query.GroupId))
            .Select(m =>
            {
                var ourSide = m.GroupId == query.GroupId ? MatchTeam.Home : MatchTeam.Away;
                var outcome = MatchScore.OutcomeFor(m.HomeScore!.Value, m.AwayScore!.Value, ourSide).ToString();
                var opponentId = ourSide == MatchTeam.Home ? m.OpponentGroupId : m.GroupId;
                var opponentName = ourSide == MatchTeam.Home ? m.OpponentGroupName : m.GroupName;
                return (Outcome: outcome, OpponentId: opponentId, OpponentName: opponentName);
            })
            .ToList();

        var opponents = played
            .Where(x => x.OpponentId.HasValue)
            .GroupBy(x => x.OpponentId!.Value)
            .Select(g => new GroupOpponentStat(
                g.Key,
                g.Last().OpponentName ?? string.Empty,
                g.Count(),
                g.Count(x => x.Outcome == "Win"),
                g.Count(x => x.Outcome == "Draw"),
                g.Count(x => x.Outcome == "Loss")))
            .OrderByDescending(o => o.Played)
            .ToList();

        return new GroupStatsResponse(
            played.Count,
            played.Count(x => x.Outcome == "Win"),
            played.Count(x => x.Outcome == "Draw"),
            played.Count(x => x.Outcome == "Loss"),
            opponents);
    }
}
