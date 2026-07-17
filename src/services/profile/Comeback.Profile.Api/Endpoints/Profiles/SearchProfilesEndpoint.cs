namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.SearchProfiles;
using MediatR;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class SearchProfilesEndpoint
{
    public static async Task<IResult> Handle(
        string query,
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var userId = httpContext.User.GetUserId();
        var result = await sender.Send(new SearchProfilesQuery(query, userId), ct);
        return Results.Ok(result);
    }
}
