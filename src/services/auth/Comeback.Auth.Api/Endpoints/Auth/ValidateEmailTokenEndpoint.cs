namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Queries.ValidateEmailToken;
using MediatR;

public static class ValidateEmailTokenEndpoint
{
    public static async Task<IResult> Handle(
        string userId,
        string token,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var isValid = await sender.Send(new ValidateEmailTokenQuery(userId, token), cancellationToken);
        return Results.Ok(new { isValid });
    }
}
