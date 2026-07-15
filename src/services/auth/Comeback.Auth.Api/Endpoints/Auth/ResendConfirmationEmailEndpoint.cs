namespace Comeback.Auth.Api.Endpoints.Auth;

using Comeback.Auth.Application.Features.Auth.Commands.ResendConfirmationEmail;
using MediatR;

public static class ResendConfirmationEmailEndpoint
{
    public sealed record Request(string Email);

    public static async Task<IResult> Handle(
        Request body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ResendConfirmationEmailCommand(body.Email), cancellationToken);
        return Results.Ok();
    }
}
