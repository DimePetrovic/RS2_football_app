namespace Comeback.Match.Application.Features.Matches.Queries.GetMatchDetails;

using Comeback.Match.Application.DTOs;
using MediatR;

// RequestingUserId is optional — when provided, the response includes MyXpChange for that player.
public sealed record GetMatchDetailsQuery(Guid MatchId, Guid? RequestingUserId = null)
    : IRequest<MatchDetailResponse>;
