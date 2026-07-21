namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowCounts;
using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class GetFollowCountsQueryHandler : IRequestHandler<GetFollowCountsQuery, FollowCountsResponse>
{
    private readonly IPlayerFollowRepository _follows;

    public GetFollowCountsQueryHandler(IPlayerFollowRepository follows) => _follows = follows;

    public async Task<FollowCountsResponse> Handle(GetFollowCountsQuery query, CancellationToken ct)
        => new(
            await _follows.CountFollowersAsync(query.UserId, ct),
            await _follows.CountFollowingAsync(query.UserId, ct));
}
