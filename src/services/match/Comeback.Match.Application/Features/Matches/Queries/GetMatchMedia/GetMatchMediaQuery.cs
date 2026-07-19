namespace Comeback.Match.Application.Features.Matches.Queries.GetMatchMedia;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetMatchMediaQuery(Guid MatchId) : IRequest<IReadOnlyList<MatchMediaResponse>>;
