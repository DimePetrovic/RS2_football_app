namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetProfileByUserId;
using MediatR;
using Microsoft.AspNetCore.Http;

public static class GetProfileByUserIdEndpoint
{
    public static async Task<IResult> Handle(
        Guid userId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProfileByUserIdQuery(userId), ct);
        return Results.Ok(result);
    }
}
