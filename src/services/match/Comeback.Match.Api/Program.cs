using System.Text;
using System.Text.Json.Serialization;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
using Comeback.Match.Infrastructure.Jobs;
using Hangfire;
using Comeback.Match.Application;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Application.Features.Matches.Commands.AssignTeams;
using Comeback.Match.Application.Features.Matches.Commands.CancelMatch;
using Comeback.Match.Application.Features.Matches.Commands.CreateMatch;
using Comeback.Match.Application.Features.Matches.Commands.InviteAdditionalPlayers;
using Comeback.Match.Application.Features.Matches.Commands.RemoveParticipant;
using Comeback.Match.Application.Features.Matches.Commands.JoinViaPublicCall;
using Comeback.Match.Application.Features.Matches.Commands.RequestPlayers;
using Comeback.Match.Application.Features.Matches.Commands.RespondToInvitation;
using Comeback.Match.Application.Features.Matches.Commands.RespondToGroupInvite;
using Comeback.Match.Application.Features.Matches.Commands.UpdateMatchDetails;
using Comeback.Match.Application.Features.Matches.Commands.SubmitResult;
using Comeback.Match.Application.Features.Matches.Commands.WithdrawFromMatch;
using Comeback.Match.Application.Features.Matches.Commands.SubmitReview;
using Comeback.Match.Application.Features.Matches.Commands.AddMatchMedia;
using Comeback.Match.Application.Features.Matches.Commands.DeleteMatchMedia;
using Comeback.Match.Application.Features.Matches.Commands.RequestMediaUpload;
using Comeback.Match.Application.Features.Matches.Queries.GetMatchMedia;
using Comeback.Match.Application.Features.Matches.Queries.GetPlayerStats;
using Comeback.Match.Application.Features.Matches.Queries.GetPlayedWithMatches;
using Comeback.Match.Application.Features.Matches.Queries.GetGroupStats;
using Comeback.Match.Domain.Enums;
using Comeback.Match.Application.Features.Matches.Queries.GetGroupMatchHistory;
using Comeback.Match.Application.Features.Matches.Queries.GetMatchDetails;
using Comeback.Match.Application.Features.Matches.Queries.GetMatchReviews;
using Comeback.Match.Application.Features.Matches.Queries.GetMyMatches;
using Comeback.Match.Application.Features.Matches.Queries.GetPlayerMatchHistory;
using Comeback.Match.Application.Features.Matches.Queries.GetPlayerReceivedReviews;
using Comeback.Match.Domain;
using Comeback.Match.Infrastructure;
using Comeback.Match.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Comeback.BuildingBlocks.Domain.Constants;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

    builder.Services.AddBuildingBlocks(ApplicationAssembly.Assembly, DomainAssembly.Assembly);
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MatchDbContext>();
        db.Database.Migrate();

        // Daily sweep at 12:00 (UTC): escalates matches with no entered result.
        // Via IRecurringJobManager (DI storage), not the static JobStorage.Current.
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<MatchReminderJob>(
            "daily-overdue-sweep", j => j.ProcessOverdueMatches(), "0 12 * * *");
    }

    app.UseBuildingBlocks();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { service = "match", status = "healthy" }))
        .WithTags("Health");

    // Internal, service-to-service only (no gateway exposure, no auth — same pattern as Rating's players endpoint).
    var matchesInternal = app.MapGroup("/api/matches/internal");

    matchesInternal.MapGet("/{id:guid}/details", async (Guid id, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetMatchDetailsQuery(id), ct)));

    matchesInternal.MapGet("/{id:guid}/reviews", async (Guid id, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetMatchReviewsQuery(id), ct)));

    var matches = app.MapGroup("/api/matches").RequireAuthorization();

    matches.MapGet("/", async (HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetMyMatchesQuery(userId), ct);
        return Results.Ok(result);
    });

    matches.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var result = await sender.Send(new GetMatchDetailsQuery(id, GetUserId(ctx)), ct);
        return Results.Ok(result);
    });

    matches.MapPost("/", async (CreateMatchRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        if (IsAdmin(ctx)) return AdminForbidden();
        var userId = GetUserId(ctx);
        var displayName = GetDisplayName(ctx);
        var matchId = await sender.Send(new CreateMatchCommand(
            req.Title,
            req.Type,
            userId,
            displayName,
            req.Location,
            req.StartsAt,
            req.DurationMinutes,
            req.PlayersPerTeam,
            req.MaxSubstitutes,
            req.Invitees.Select(i => new InviteeDto(i.UserId, i.DisplayName)).ToList(),
            req.GroupId,
            req.OpponentGroupId,
            req.GuestNames), ct);
        return Results.Created($"/api/matches/{matchId}", new { id = matchId });
    });

    matches.MapPost("/{id:guid}/group-invite/respond", async (Guid id, RespondRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        if (IsAdmin(ctx)) return AdminForbidden();
        var userId = GetUserId(ctx);
        await sender.Send(new RespondToGroupInviteCommand(id, userId, req.Accept), ct);
        return Results.NoContent();
    });

    matches.MapGet("/groups/{groupId:guid}/history", async (Guid groupId, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetGroupMatchHistoryQuery(groupId), ct)));

    matches.MapPost("/{id:guid}/join", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        if (IsAdmin(ctx)) return AdminForbidden();
        await sender.Send(new JoinViaPublicCallCommand(id, GetUserId(ctx), GetDisplayName(ctx)), ct);
        return Results.NoContent();
    });

    matches.MapPost("/{id:guid}/request-players", async (Guid id, RequestPlayersRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        if (IsAdmin(ctx)) return AdminForbidden();
        await sender.Send(new RequestPlayersCommand(id, GetUserId(ctx), GetDisplayName(ctx), req.Position), ct);
        return Results.NoContent();
    });

    matches.MapPost("/{id:guid}/respond", async (Guid id, RespondRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        if (IsAdmin(ctx)) return AdminForbidden();
        var userId = GetUserId(ctx);
        var displayName = GetDisplayName(ctx);
        await sender.Send(new RespondToInvitationCommand(id, userId, displayName, req.Accept), ct);
        return Results.NoContent();
    });

    matches.MapPost("/{id:guid}/withdraw", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var displayName = GetDisplayName(ctx);
        await sender.Send(new WithdrawFromMatchCommand(id, userId, displayName), ct);
        return Results.NoContent();
    });

    matches.MapPost("/{id:guid}/result", async (Guid id, SubmitResultRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var goals = req.Goals.Select(g => new GoalEntryDto(g.ScorerUserId, g.IsOwnGoal, g.AssistUserId)).ToList();
        await sender.Send(new SubmitResultCommand(id, userId, req.HomeScore, req.AwayScore, goals), ct);
        return Results.NoContent();
    });

    matches.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        await sender.Send(new CancelMatchCommand(id, userId), ct);
        return Results.NoContent();
    });

    matches.MapPut("/{id:guid}", async (Guid id, UpdateMatchDetailsRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        await sender.Send(new UpdateMatchDetailsCommand(
            id, userId, req.Title, req.Location, req.StartsAt, req.DurationMinutes), ct);
        return Results.NoContent();
    });

    matches.MapPost("/{id:guid}/invite", async (Guid id, InvitePlayersRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var displayName = GetDisplayName(ctx);
        await sender.Send(new InviteAdditionalPlayersCommand(
            id, userId, displayName,
            req.Invitees.Select(i => new InviteeDto(i.UserId, i.DisplayName)).ToList(),
            req.GuestNames), ct);
        return Results.NoContent();
    });

    matches.MapDelete("/{id:guid}/participants/{targetUserId:guid}", async (
        Guid id, Guid targetUserId, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new RemoveParticipantCommand(id, GetUserId(ctx), targetUserId), ct);
        return Results.NoContent();
    });

    var teams = matches.MapGroup("/{id:guid}/teams");

    teams.MapPost("/assign", async (Guid id, AssignPlayerRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        await sender.Send(new AssignPlayerToTeamCommand(id, userId, req.TargetUserId, req.Team), ct);
        return Results.NoContent();
    });

    teams.MapPost("/randomize", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new RandomizeTeamsCommand(id, GetUserId(ctx)), ct);
        return Results.NoContent();
    });

    teams.MapPost("/randomize-captains", async (Guid id, RandomizeCaptainsRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new RandomizeTeamsWithCaptainsCommand(id, GetUserId(ctx), req.HomeCaptainId, req.AwayCaptainId), ct);
        return Results.NoContent();
    });

    teams.MapPost("/balance", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new BalanceTeamsCommand(id, GetUserId(ctx)), ct);
        return Results.NoContent();
    });

    matches.MapGet("/players/{userId:guid}/history", async (Guid userId, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetPlayerMatchHistoryQuery(userId), ct)));

    matches.MapGet("/players/{userId:guid}/reviews", async (Guid userId, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetPlayerReceivedReviewsQuery(userId), ct)));

    matches.MapGet("/players/{userId:guid}/stats", async (Guid userId, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetPlayerStatsQuery(userId), ct)));

    matches.MapGet("/players/{userId:guid}/stats/matches", async (
        Guid userId, Guid withId, PlayedWithRelation relation, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetPlayedWithMatchesQuery(userId, withId, relation), ct)));

    matches.MapGet("/groups/{groupId:guid}/stats", async (Guid groupId, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetGroupStatsQuery(groupId), ct)));

    matches.MapGet("/{id:guid}/reviews", async (Guid id, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetMatchReviewsQuery(id), ct)));

    var media = matches.MapGroup("/{id:guid}/media");

    media.MapGet("/", async (Guid id, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new GetMatchMediaQuery(id), ct)));

    media.MapPost("/upload-signature", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new RequestMediaUploadCommand(id, GetUserId(ctx)), ct)));

    media.MapPost("/", async (Guid id, AddMatchMediaRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var result = await sender.Send(new AddMatchMediaCommand(
            id, GetUserId(ctx), req.MediaType, req.StoragePublicId, req.Url, req.ThumbnailUrl,
            req.Format, req.SizeInBytes, req.DurationInSeconds, req.Width, req.Height), ct);
        return Results.Created($"/api/matches/{id}/media/{result.Id}", result);
    });

    media.MapDelete("/{mediaId:guid}", async (Guid id, Guid mediaId, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new DeleteMatchMediaCommand(id, mediaId, GetUserId(ctx)), ct);
        return Results.NoContent();
    });

    matches.MapPost("/{id:guid}/reviews", async (Guid id, SubmitReviewRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new SubmitReviewCommand(
            id, GetUserId(ctx), req.ReviewedParticipantId,
            req.OverallRating, req.GoalkeepingRating, req.DefenseRating,
            req.AttackRating, req.EffortRating, req.Comment), ct);
        return Results.NoContent();
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Match service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static Guid GetUserId(HttpContext ctx) => ctx.GetUserId();

// Admin is not a player and does not participate in matches (admin role is outside the sports domain).
static bool IsAdmin(HttpContext ctx) => ctx.User.IsInRole(UserRoles.Admin);

static IResult AdminForbidden()
    => Results.Problem(statusCode: StatusCodes.Status403Forbidden,
        detail: "Administrators cannot participate in matches.",
        extensions: new Dictionary<string, object?> { ["code"] = "match.admin_forbidden" });

static string GetDisplayName(HttpContext ctx) => ctx.GetDisplayName();

record CreateMatchInviteeDto(Guid UserId, string DisplayName);

record CreateMatchRequest(
    string Title,
    Comeback.Match.Domain.Enums.MatchType Type,
    string? Location,
    DateTime StartsAt,
    int? DurationMinutes,
    int PlayersPerTeam,
    int MaxSubstitutes,
    IReadOnlyList<CreateMatchInviteeDto> Invitees,
    Guid? GroupId,
    Guid? OpponentGroupId,
    IReadOnlyList<string>? GuestNames);

record UpdateMatchDetailsRequest(
    string Title,
    string? Location,
    DateTime StartsAt,
    int? DurationMinutes);

record InvitePlayersRequest(
    IReadOnlyList<CreateMatchInviteeDto> Invitees,
    IReadOnlyList<string>? GuestNames);

record RequestPlayersRequest(string? Position);
record RespondRequest(bool Accept);
record GoalEntryRequest(Guid ScorerUserId, bool IsOwnGoal, Guid? AssistUserId);
record SubmitResultRequest(int HomeScore, int AwayScore, IReadOnlyList<GoalEntryRequest> Goals);
record AssignPlayerRequest(Guid TargetUserId, MatchTeam Team);
record RandomizeCaptainsRequest(Guid HomeCaptainId, Guid AwayCaptainId);
record AddMatchMediaRequest(
    MatchMediaType MediaType,
    string StoragePublicId,
    string Url,
    string? ThumbnailUrl,
    string? Format,
    long? SizeInBytes,
    double? DurationInSeconds,
    int? Width,
    int? Height);
record SubmitReviewRequest(
    Guid ReviewedParticipantId,
    decimal OverallRating,
    decimal? GoalkeepingRating,
    decimal? DefenseRating,
    decimal? AttackRating,
    decimal? EffortRating,
    string? Comment);

// Marker for integration tests (WebApplicationFactory<Program>).
public partial class Program;
