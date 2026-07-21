namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetFollowing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class GetFollowingEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var currentUserId = httpContext.User.GetUserId();
        var result = await sender.Send(new GetFollowingQuery(currentUserId), ct);
        return Results.Ok(result);
    }
}
