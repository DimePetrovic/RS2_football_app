namespace Comeback.Match.Application.Features.Matches.Queries.GetMyMatches;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetMyMatchesQuery(Guid UserId) : IRequest<List<MatchSummaryResponse>>;
