namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowing;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, List<ProfileSearchResult>>
{
    private readonly IPlayerFollowRepository _follows;
    private readonly IUserProfileRepository _profiles;

    public GetFollowingQueryHandler(IPlayerFollowRepository follows, IUserProfileRepository profiles)
    {
        _follows = follows;
        _profiles = profiles;
    }

    public async Task<List<ProfileSearchResult>> Handle(GetFollowingQuery query, CancellationToken cancellationToken)
    {
        var followedIds = await _follows.GetFollowingIdsAsync(query.UserId, cancellationToken);
        if (followedIds.Count == 0) return [];

        var profiles = await _profiles.GetByUserIdsAsync(followedIds, cancellationToken);
        return profiles.Select(p => new ProfileSearchResult(
            p.UserId, p.Username, p.FirstName, p.LastName, p.DisplayName, p.AvatarUrl, p.Nationality)).ToList();
    }
}
