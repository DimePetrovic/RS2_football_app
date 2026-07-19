namespace Comeback.Match.Application.Features.Matches.Queries.GetGroupStats;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record GetGroupStatsQuery(Guid GroupId) : IRequest<GroupStatsResponse>;
