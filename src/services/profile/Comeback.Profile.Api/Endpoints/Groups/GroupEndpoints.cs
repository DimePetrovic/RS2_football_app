namespace Comeback.Profile.Api.Endpoints.Groups;

using Comeback.Profile.Application.Features.Groups.Commands.AddGroupMember;
using Comeback.Profile.Application.Features.Groups.Commands.CreateGroup;
using Comeback.Profile.Application.Features.Groups.Commands.DeleteGroup;
using Comeback.Profile.Application.Features.Groups.Commands.LeaveGroup;
using Comeback.Profile.Application.Features.Groups.Commands.RemoveGroupMember;
using Comeback.Profile.Application.Features.Groups.Commands.UpdateGroup;
using Comeback.Profile.Application.Features.Groups.Queries.GetGroupById;
using Comeback.Profile.Application.Features.Groups.Queries.GetGroupMatchInfo;
using Comeback.Profile.Application.Features.Groups.Queries.GetMyGroups;
using Comeback.Profile.Application.Features.Groups.Queries.SearchGroups;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

public static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        // Internal, service-to-service only (no gateway exposure, no auth — same pattern as Rating's players endpoint).
        app.MapGet("/api/groups/internal/{groupId:guid}/match-info", GetGroupMatchInfo)
            .WithName("GetGroupMatchInfo");

        var group = app.MapGroup("/api/groups").WithTags("Groups").RequireAuthorization();

        group.MapGet("/mine", GetMyGroups)
            .WithName("GetMyGroups");

        group.MapGet("/search", SearchGroups)
            .WithName("SearchGroups");

        group.MapGet("/{groupId:guid}", GetGroupById)
            .WithName("GetGroupById");

        group.MapPost("/", CreateGroup)
            .WithName("CreateGroup");

        group.MapPut("/{groupId:guid}", UpdateGroup)
            .WithName("UpdateGroup");

        group.MapPost("/{groupId:guid}/members", AddMember)
            .WithName("AddGroupMember");

        group.MapDelete("/{groupId:guid}/members/{memberUserId:guid}", RemoveMember)
            .WithName("RemoveGroupMember");

        group.MapPost("/{groupId:guid}/leave", LeaveGroup)
            .WithName("LeaveGroup");

        group.MapDelete("/{groupId:guid}", DeleteGroup)
            .WithName("DeleteGroup");

        return app;
    }

    private static async Task<IResult> GetMyGroups(HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetMyGroupsQuery(userId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetGroupById(Guid groupId, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetGroupByIdQuery(groupId, userId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchGroups(string query, Guid? excludeOverlappingWithGroupId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new SearchGroupsQuery(query, excludeOverlappingWithGroupId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetGroupMatchInfo(Guid groupId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetGroupMatchInfoQuery(groupId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateGroup(
        [FromBody] CreateGroupRequest request,
        HttpContext ctx,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new CreateGroupCommand(userId, request.Name, request.AvatarUrl, request.MemberUserIds), ct);
        return Results.Created($"/api/groups/{result.Id}", result);
    }

    private static async Task<IResult> UpdateGroup(
        Guid groupId,
        [FromBody] UpdateGroupRequest request,
        HttpContext ctx,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        await sender.Send(new UpdateGroupCommand(groupId, userId, request.Name, request.AvatarUrl), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddMember(
        Guid groupId,
        [FromBody] AddMemberRequest request,
        HttpContext ctx,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        await sender.Send(new AddGroupMemberCommand(groupId, userId, request.MemberUserId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveMember(
        Guid groupId,
        Guid memberUserId,
        HttpContext ctx,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        await sender.Send(new RemoveGroupMemberCommand(groupId, userId, memberUserId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> LeaveGroup(
        Guid groupId,
        HttpContext ctx,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        await sender.Send(new LeaveGroupCommand(groupId, userId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteGroup(
        Guid groupId,
        HttpContext ctx,
        ISender sender,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        await sender.Send(new DeleteGroupCommand(groupId, userId), ct);
        return Results.NoContent();
    }

    private static Guid GetUserId(HttpContext ctx)
        => ctx.GetUserId();
}

public sealed record CreateGroupRequest(string Name, string? AvatarUrl, List<Guid> MemberUserIds);
public sealed record UpdateGroupRequest(string Name, string? AvatarUrl);
public sealed record AddMemberRequest(Guid MemberUserId);
