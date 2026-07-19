namespace Comeback.Match.Application.Features.Matches.Queries.GetMatchReviews;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetMatchReviewsQuery(Guid MatchId) : IRequest<IReadOnlyList<MatchReviewResponse>>;
