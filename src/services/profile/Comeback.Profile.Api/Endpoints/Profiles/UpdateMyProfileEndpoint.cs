namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.Profile.Application.Features.Profiles.Commands.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class UpdateMyProfileEndpoint
{
    public sealed record Request(
        string? DisplayName,
        string? Bio,
        string? AvatarUrl,
        string? Position,
        bool? CanPlayGoalkeeper,
        string? SkillLevel,
        string? Nationality);

    public static async Task<IResult> Handle(
        Request req,
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var userId = httpContext.User.GetUserId();

        var command = new UpdateProfileCommand(
            userId,
            req.DisplayName,
            req.Bio,
            req.AvatarUrl,
            req.Position,
            req.CanPlayGoalkeeper,
            req.SkillLevel,
            req.Nationality);

        var result = await sender.Send(command, ct);
        return Results.Ok(result);
    }
}
