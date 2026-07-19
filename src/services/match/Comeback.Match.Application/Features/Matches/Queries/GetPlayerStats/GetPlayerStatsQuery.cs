namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayerStats;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetPlayerStatsQuery(Guid UserId) : IRequest<PlayerStatsResponse>;
