namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayerMatchHistory;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using MediatR;

public sealed class GetPlayerMatchHistoryQueryHandler
    : IRequestHandler<GetPlayerMatchHistoryQuery, List<PlayerMatchHistoryItem>>
{
    private readonly IMatchRepository _matches;

    public GetPlayerMatchHistoryQueryHandler(IMatchRepository matches)
        => _matches = matches;

    public async Task<List<PlayerMatchHistoryItem>> Handle(
        GetPlayerMatchHistoryQuery query, CancellationToken ct)
    {
        var matches = await _matches.GetByUserIdAsync(query.UserId, ct);
        // The profile only shows matches with an entered result.
        return matches
            .Where(m => m.HomeScore.HasValue && m.AwayScore.HasValue)
            .Select(m =>
            {
                var participant = m.Participants.FirstOrDefault(p => p.UserId == query.UserId);
                return new PlayerMatchHistoryItem(
                    m.Id, m.Title, m.Status.ToString(), m.StartsAt,
                    m.HomeScore, m.AwayScore,
                    participant?.Team.ToString() ?? "None");
            }).ToList();
    }
}
