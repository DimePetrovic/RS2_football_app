namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayerReceivedReviews;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetPlayerReceivedReviewsQuery(Guid UserId) : IRequest<IReadOnlyList<PlayerReceivedReviewItem>>;
