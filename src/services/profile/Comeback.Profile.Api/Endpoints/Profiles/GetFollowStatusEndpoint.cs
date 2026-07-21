namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetFollowStatus;
using MediatR;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class GetFollowStatusEndpoint
{
    public static async Task<IResult> Handle(
        Guid userId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var currentUserId = httpContext.User.GetUserId();
        var isFollowing = await sender.Send(new GetFollowStatusQuery(currentUserId, userId), ct);
        return Results.Ok(new { isFollowing });
    }
}
