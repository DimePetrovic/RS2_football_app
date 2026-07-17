namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Queries.GetAllUserIds;
using MediatR;
using Microsoft.AspNetCore.Http;

public static class GetAllUserIdsEndpoint
{
    public static async Task<IResult> Handle(ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetAllUserIdsQuery(), ct));
}
