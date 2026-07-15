namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Commands.Register;
using MediatR;

public static class RegisterEndpoint
{
    public sealed record Request(string Email, string Username, string Password, string ConfirmPassword);

    public static async Task<IResult> Handle(
        Request request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Username,
            request.Password,
            request.ConfirmPassword,
            CookieHelper.GetClientIp(httpContext));

        var result = await sender.Send(command, cancellationToken);

        return Results.Accepted(value: new { result.Message });
    }
}
