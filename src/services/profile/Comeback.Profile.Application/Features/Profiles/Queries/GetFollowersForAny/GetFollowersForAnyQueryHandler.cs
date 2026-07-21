namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowersForAny;

using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class GetFollowersForAnyQueryHandler : IRequestHandler<GetFollowersForAnyQuery, List<Guid>>
{
    private readonly IPlayerFollowRepository _follows;

    public GetFollowersForAnyQueryHandler(IPlayerFollowRepository follows) => _follows = follows;

    public Task<List<Guid>> Handle(GetFollowersForAnyQuery query, CancellationToken cancellationToken)
        => _follows.GetFollowerIdsForAnyAsync(query.UserIds, cancellationToken);
}
