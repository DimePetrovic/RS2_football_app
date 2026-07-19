namespace Comeback.Match.Application.Features.Matches.Queries.GetMatchReviews;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using MediatR;

public sealed class GetMatchReviewsQueryHandler
    : IRequestHandler<GetMatchReviewsQuery, IReadOnlyList<MatchReviewResponse>>
{
    private readonly IMatchReviewRepository _reviews;

    public GetMatchReviewsQueryHandler(IMatchReviewRepository reviews)
        => _reviews = reviews;

    public async Task<IReadOnlyList<MatchReviewResponse>> Handle(
        GetMatchReviewsQuery query, CancellationToken ct)
    {
        var reviews = await _reviews.GetByMatchAsync(query.MatchId, ct);
        return reviews
            .Select(r => new MatchReviewResponse(
                r.ReviewerParticipantId,
                r.ReviewedParticipantId,
                r.OverallRating,
                r.GoalkeepingRating,
                r.DefenseRating,
                r.AttackRating,
                r.EffortRating,
                r.Comment,
                r.CreatedAt))
            .ToList();
    }
}
