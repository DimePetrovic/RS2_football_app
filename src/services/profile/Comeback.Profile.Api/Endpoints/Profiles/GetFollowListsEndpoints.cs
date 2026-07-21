namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetFollowCounts;
using Comeback.Profile.Application.Features.Profiles.Queries.GetFollowers;
using Comeback.Profile.Application.Features.Profiles.Queries.GetFollowing;
using MediatR;
using Microsoft.AspNetCore.Http;

/// <summary>Follower/following counts and lists for any player's profile page.</summary>
public static class GetFollowListsEndpoints
{
    public static async Task<IResult> Counts(Guid userId, ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetFollowCountsQuery(userId), ct));

    public static async Task<IResult> Followers(Guid userId, ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetFollowersQuery(userId), ct));

    public static async Task<IResult> Following(Guid userId, ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetFollowingQuery(userId), ct));
}
