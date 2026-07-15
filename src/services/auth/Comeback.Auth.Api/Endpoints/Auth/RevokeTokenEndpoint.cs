namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Commands.Revoke;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;

public static class RevokeTokenEndpoint
{
    public static async Task<IResult> Handle(
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var token = httpContext.Request.Cookies["X-Refresh-Token"]
            ?? throw new NotFoundException("Refresh token cookie is missing.", "auth.refresh_token_missing");

        var command = new RevokeTokenCommand(token, GetClientIp(httpContext));
        await sender.Send(command, cancellationToken);

        httpContext.Response.Cookies.Delete("X-Refresh-Token");

        return Results.NoContent();
    }

    private static string GetClientIp(HttpContext context) => CookieHelper.GetClientIp(context);
}
