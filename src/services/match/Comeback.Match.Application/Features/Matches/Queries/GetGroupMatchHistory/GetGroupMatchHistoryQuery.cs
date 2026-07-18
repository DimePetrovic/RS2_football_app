namespace Comeback.Match.Application.Features.Matches.Queries.GetGroupMatchHistory;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetGroupMatchHistoryQuery(Guid GroupId) : IRequest<List<MatchSummaryResponse>>;
