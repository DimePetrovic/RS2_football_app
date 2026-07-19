namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayerMatchHistory;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetPlayerMatchHistoryQuery(Guid UserId) : IRequest<List<PlayerMatchHistoryItem>>;
