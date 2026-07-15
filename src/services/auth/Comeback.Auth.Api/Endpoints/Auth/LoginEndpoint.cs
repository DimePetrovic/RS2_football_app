namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Commands.Login;
using MediatR;

public static class LoginEndpoint
{
    public sealed record Request(string Email, string Password);

    public static async Task<IResult> Handle(
        Request request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            GetClientIp(httpContext));

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
