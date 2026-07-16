namespace Comeback.Match.Application.Features.Matches.Queries.GetMyMatches;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class GetMyMatchesQueryHandler : IRequestHandler<GetMyMatchesQuery, List<MatchSummaryResponse>>
{
    private readonly IMatchRepository _matches;

    public GetMyMatchesQueryHandler(IMatchRepository matches)
        => _matches = matches;

    public async Task<List<MatchSummaryResponse>> Handle(GetMyMatchesQuery query, CancellationToken ct)
    {
        var matches = await _matches.GetByUserIdAsync(query.UserId, ct);

        return matches.Select(m => new MatchSummaryResponse(
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
