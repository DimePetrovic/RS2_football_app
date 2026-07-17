namespace Comeback.Profile.Api.Endpoints.Profiles;

using Comeback.BuildingBlocks.Infrastructure.Media;
using Microsoft.AspNetCore.Http;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class CreateAvatarUploadSignatureEndpoint
{
    public static IResult Handle(
        HttpContext httpContext,
        ICloudinaryMediaService cloudinary)
    {
        var userId = httpContext.User.GetUserId();
        var signature = cloudinary.CreateUploadSignature($"comeback/avatars/{userId}");
        return Results.Ok(signature);
    }
}
