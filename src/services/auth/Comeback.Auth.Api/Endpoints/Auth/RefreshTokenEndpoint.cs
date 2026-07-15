namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Commands.RefreshToken;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;

public static class RefreshTokenEndpoint
{
    public static async Task<IResult> Handle(
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var token = httpContext.Request.Cookies["X-Refresh-Token"]
            ?? throw new NotFoundException("Refresh token cookie is missing.", "auth.refresh_token_missing");

        var command = new RefreshTokenCommand(token, GetClientIp(httpContext));
        var result = await sender.Send(command, cancellationToken);

        SetRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAt);

        return Results.Ok(new
        {
            result.AccessToken,
            result.AccessTokenExpiresAt,
            result.UserId,
            result.Username,
            result.Email,
            result.Role,
        });
    }

    private static string GetClientIp(HttpContext context) => CookieHelper.GetClientIp(context);

    private static void SetRefreshTokenCookie(HttpContext context, string token, DateTime expiresAt) =>
        CookieHelper.SetRefreshTokenCookie(context, token, expiresAt);
}
