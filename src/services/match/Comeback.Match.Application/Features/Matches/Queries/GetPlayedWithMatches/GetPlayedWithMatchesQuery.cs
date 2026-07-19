namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayedWithMatches;

using Comeback.Match.Application.DTOs;
using MediatR;

public enum PlayedWithRelation
{
    /// <summary>All matches played together.</summary>
    All,

    /// <summary>Only matches where they were on the same team.</summary>
    Teammate,

    /// <summary>Only matches where they were opponents.</summary>
    Opponent,
}

public sealed record GetPlayedWithMatchesQuery(
    Guid UserId,
    Guid WithUserId,
    PlayedWithRelation Relation) : IRequest<List<PlayerMatchHistoryItem>>;
