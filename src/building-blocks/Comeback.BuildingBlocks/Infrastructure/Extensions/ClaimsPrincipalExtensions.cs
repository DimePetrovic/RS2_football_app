namespace Comeback.BuildingBlocks.Infrastructure.Extensions;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

/// <summary>
/// One place to read identity from the JWT — all services use the same claim types.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static Guid GetUserId(this HttpContext context)
        => context.User.GetUserId();

    public static string GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name)!;

    public static string GetDisplayName(this HttpContext context)
        => context.User.GetDisplayName();
}
