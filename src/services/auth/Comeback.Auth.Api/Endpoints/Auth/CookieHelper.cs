namespace Comeback.Auth.Api.Endpoints.Auth;

internal static class CookieHelper
{
    internal const string RefreshTokenCookieName = "X-Refresh-Token";

    internal static string GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    internal static void SetRefreshTokenCookie(HttpContext context, string token, DateTime expiresAt) =>
        context.Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
        });
}
