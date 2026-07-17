namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetProfileByUserId;
using MediatR;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class GetMyProfileEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var userId = httpContext.User.GetUserId();
        var result = await sender.Send(new GetProfileByUserIdQuery(userId), ct);
        return Results.Ok(result);
    }
}
