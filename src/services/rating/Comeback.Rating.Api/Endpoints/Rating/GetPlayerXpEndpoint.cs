namespace Comeback.Rating.Api.Endpoints.Rating;

using Comeback.Rating.Application.Features.Xp.Queries.GetPlayerXp;
using MediatR;

public static class GetPlayerXpEndpoint
{
    public static async Task<IResult> Handle(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPlayerXpQuery(userId), cancellationToken);
        return Results.Ok(result);
    }
}
