namespace Comeback.Match.Application.Features.Matches.Queries.GetGroupMatchHistory;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class GetGroupMatchHistoryQueryHandler : IRequestHandler<GetGroupMatchHistoryQuery, List<MatchSummaryResponse>>
{
    private readonly IMatchRepository _matches;

    public GetGroupMatchHistoryQueryHandler(IMatchRepository matches) => _matches = matches;

    public async Task<List<MatchSummaryResponse>> Handle(GetGroupMatchHistoryQuery query, CancellationToken ct)
    {
        var matches = await _matches.GetByGroupIdAsync(query.GroupId, ct);

        return matches
            .OrderByDescending(m => m.StartsAt)
            .Select(m => new MatchSummaryResponse(
                m.Id,
                m.Title,
                m.Type.ToString(),
                m.Status.ToString(),
                m.OrganizerUserId,
                m.Location,
                m.StartsAt,
                m.DurationMinutes,
                m.PlayersPerTeam,
                m.Participants.Count(p => p.Status == MatchParticipantStatus.Accepted),
                m.CreatedAt)).ToList();
    }
}
