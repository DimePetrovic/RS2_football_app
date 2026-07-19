namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayedWithMatches;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class GetPlayedWithMatchesQueryHandler
    : IRequestHandler<GetPlayedWithMatchesQuery, List<PlayerMatchHistoryItem>>
{
    private readonly IMatchRepository _matches;

    public GetPlayedWithMatchesQueryHandler(IMatchRepository matches)
        => _matches = matches;

    public async Task<List<PlayerMatchHistoryItem>> Handle(
        GetPlayedWithMatchesQuery query, CancellationToken ct)
    {
        var matches = await _matches.GetByUserIdAsync(query.UserId, ct);

        return matches
            .Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue)
            .Select(m => (
                Match: m,
                Me: AcceptedParticipant(m, query.UserId),
                Other: AcceptedParticipant(m, query.WithUserId)))
            .Where(x => x.Me is not null && x.Other is not null
                     && MatchesRelation(x.Me!, x.Other!, query.Relation))
            .OrderByDescending(x => x.Match.StartsAt)
            .Select(x => new PlayerMatchHistoryItem(
                x.Match.Id, x.Match.Title, x.Match.Status.ToString(), x.Match.StartsAt,
                x.Match.HomeScore, x.Match.AwayScore,
                x.Me!.Team.ToString()))
            .ToList();
    }

    // Same rule as "played" in statistics: accepted AND assigned to a team.
    // Thus always All = Teammates + Opponents (a participant without a team did not play the match).
    private static MatchParticipant? AcceptedParticipant(Match m, Guid userId)
        => m.Participants.FirstOrDefault(
            p => p.UserId == userId
              && p.Status == MatchParticipantStatus.Accepted
              && p.Team != MatchTeam.None);

    private static bool MatchesRelation(
        MatchParticipant me, MatchParticipant other, PlayedWithRelation relation)
        => relation switch
        {
            PlayedWithRelation.Teammate => me.Team == other.Team,
            PlayedWithRelation.Opponent => me.Team != other.Team,
            _ => true,
        };
}
