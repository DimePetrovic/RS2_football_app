namespace Comeback.Profile.Api.Endpoints.Profiles;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Comeback.BuildingBlocks.Domain.Constants;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles").WithTags("Profiles");

        group.MapGet("/me", GetMyProfileEndpoint.Handle)
            .RequireAuthorization()
            .WithName("GetMyProfile");

        group.MapGet("/search", SearchProfilesEndpoint.Handle)
            .RequireAuthorization()
            .WithName("SearchProfiles");

        group.MapGet("/admin/users", GetAllUsersEndpoint.Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRoles.Admin))
            .WithName("GetAllUsersAdmin");

        group.MapGet("/{userId:guid}", GetProfileByUserIdEndpoint.Handle)
            .WithName("GetProfileByUserId");

        group.MapPut("/me", UpdateMyProfileEndpoint.Handle)
            .RequireAuthorization()
            .WithName("UpdateMyProfile");

        group.MapPost("/me/avatar/upload-signature", CreateAvatarUploadSignatureEndpoint.Handle)
            .RequireAuthorization()
            .WithName("CreateAvatarUploadSignature");

        group.MapGet("/me/following", GetFollowingEndpoint.Handle)
            .RequireAuthorization()
            .WithName("GetFollowing");

        group.MapPost("/{userId:guid}/follow", FollowPlayerEndpoint.Handle)
            .RequireAuthorization()
            .WithName("FollowPlayer");

        group.MapDelete("/{userId:guid}/follow", UnfollowPlayerEndpoint.Handle)
            .RequireAuthorization()
            .WithName("UnfollowPlayer");

        group.MapGet("/{userId:guid}/follow-counts", GetFollowListsEndpoints.Counts)
            .RequireAuthorization()
            .WithName("GetFollowCounts");

        group.MapGet("/{userId:guid}/followers", GetFollowListsEndpoints.Followers)
            .RequireAuthorization()
            .WithName("GetFollowers");

        group.MapGet("/{userId:guid}/following", GetFollowListsEndpoints.Following)
            .RequireAuthorization()
            .WithName("GetFollowingOf");

        group.MapGet("/{userId:guid}/follow-status", GetFollowStatusEndpoint.Handle)
            .RequireAuthorization()
            .WithName("GetFollowStatus");

        // Internal, service-to-service only (no gateway exposure, no auth — same pattern as Rating's players endpoint).
        group.MapGet("/internal/followers-for-any", GetFollowersForAnyEndpoint.Handle)
            .WithName("GetFollowersForAny");

        group.MapGet("/internal/all-ids", GetAllUserIdsEndpoint.Handle)
            .WithName("GetAllUserIds");

        group.MapGet("/internal/avatars", GetAvatarsForUsersEndpoint.Handle)
            .WithName("GetAvatarsForUsers");

        return app;
    }
}
