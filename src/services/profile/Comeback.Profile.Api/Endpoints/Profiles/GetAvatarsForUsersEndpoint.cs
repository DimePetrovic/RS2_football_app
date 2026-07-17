namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetAvatarsForUsers;
using MediatR;
using Microsoft.AspNetCore.Http;

public static class GetAvatarsForUsersEndpoint
{
    public static async Task<IResult> Handle(
        string userIds,
        ISender sender,
        CancellationToken ct)
    {
        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Guid.Parse)
            .ToList();
        var result = await sender.Send(new GetAvatarsForUsersQuery(ids), ct);
        return Results.Ok(result);
    }
}
