namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Commands.FollowPlayer;
using MediatR;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class FollowPlayerEndpoint
{
    public static async Task<IResult> Handle(
        Guid userId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var currentUserId = httpContext.User.GetUserId();
        await sender.Send(new FollowPlayerCommand(currentUserId, userId), ct);
        return Results.NoContent();
    }
}
