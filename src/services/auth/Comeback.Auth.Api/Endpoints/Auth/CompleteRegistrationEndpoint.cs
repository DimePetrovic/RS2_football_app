namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Commands.CompleteRegistration;
using MediatR;

public static class CompleteRegistrationEndpoint
{
    public sealed record Request(
        string UserId,
        string Token,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        int PreferredPosition,
        bool CanPlayGoalkeeper,
        int YouthSeasons,
        int SeniorSeasons,
        string? Nationality);

    public static async Task<IResult> Handle(
        Request body,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CompleteRegistrationCommand(
            body.UserId,
            body.Token,
            body.FirstName,
            body.LastName,
            body.DateOfBirth,
            body.PreferredPosition,
            body.CanPlayGoalkeeper,
            body.YouthSeasons,
            body.SeniorSeasons,
            body.Nationality,
            CookieHelper.GetClientIp(httpContext));

        var result = await sender.Send(command, cancellationToken);

        CookieHelper.SetRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAt);

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
}
