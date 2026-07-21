namespace Comeback.Profile.Application.Features.Profiles.Queries.GetFollowers;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, List<ProfileSearchResult>>
{
    private readonly IPlayerFollowRepository _follows;
    private readonly IUserProfileRepository _profiles;

    public GetFollowersQueryHandler(IPlayerFollowRepository follows, IUserProfileRepository profiles)
    {
        _follows = follows;
        _profiles = profiles;
    }

    public async Task<List<ProfileSearchResult>> Handle(GetFollowersQuery query, CancellationToken ct)
    {
        var followerIds = await _follows.GetFollowerIdsAsync(query.UserId, ct);
        if (followerIds.Count == 0) return [];

        var profiles = await _profiles.GetByUserIdsAsync(followerIds, ct);
        return profiles.Select(p => new ProfileSearchResult(
            p.UserId, p.Username, p.FirstName, p.LastName, p.DisplayName, p.AvatarUrl, p.Nationality)).ToList();
    }
}
