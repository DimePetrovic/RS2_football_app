using System.Text;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
using Comeback.Social.Application;
using Comeback.Social.Application.Features.Feed.Queries.GetFeed;
using Comeback.Social.Application.Features.Posts.Commands.AddComment;
using Comeback.Social.Application.Features.Posts.Commands.ToggleLike;
using Comeback.Social.Application.Features.Posts.Queries.GetComments;
using Comeback.Social.Application.Features.Posts.Queries.GetPostById;
using Comeback.Social.Infrastructure;
using Comeback.Social.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

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

    builder.Services.AddBuildingBlocks(ApplicationAssembly.Assembly);
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

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        db.Database.Migrate();
    }

    app.UseBuildingBlocks();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { service = "social", status = "healthy" }))
        .WithTags("Health");

    var feed = app.MapGroup("/api/feed").RequireAuthorization();

    feed.MapGet("/", async (HttpContext ctx, ISender sender, int page, int pageSize, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetFeedQuery(userId, Math.Max(page, 0), pageSize <= 0 ? 20 : Math.Min(pageSize, 50)), ct);
        return Results.Ok(result);
    });

    var posts = app.MapGroup("/api/posts").RequireAuthorization();

    posts.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetPostByIdQuery(id, userId), ct);
        return Results.Ok(result);
    });

    posts.MapPost("/{id:guid}/reactions", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var liked = await sender.Send(new ToggleLikeCommand(id, userId), ct);
        return Results.Ok(new { liked });
    });

    posts.MapGet("/{id:guid}/comments", async (Guid id, ISender sender, CancellationToken ct) =>
    {
        var result = await sender.Send(new GetCommentsQuery(id), ct);
        return Results.Ok(result);
    });

    posts.MapPost("/{id:guid}/comments", async (Guid id, AddCommentRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var displayName = GetDisplayName(ctx);
        var result = await sender.Send(new AddCommentCommand(id, userId, displayName, req.Content), ct);
        return Results.Ok(result);
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Social service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static Guid GetUserId(HttpContext ctx) => ctx.GetUserId();

static string GetDisplayName(HttpContext ctx) => ctx.GetDisplayName();

record AddCommentRequest(string Content);

// Marker for integration tests (WebApplicationFactory<Program>).
public partial class Program;
