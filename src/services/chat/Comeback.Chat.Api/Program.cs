using System.Text;
using System.Text.Json.Serialization;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
using Comeback.Chat.Api.Hubs;
using Comeback.Chat.Application;
using Comeback.Chat.Application.Features.Conversations.Commands.DeleteConversationForMe;
using Comeback.Chat.Application.Features.Conversations.Commands.GetOrCreate;
using Comeback.Chat.Application.Features.Conversations.Commands.GetOrCreateGroup;
using Comeback.Chat.Application.Features.Conversations.Queries.GetConversations;
using Comeback.Chat.Application.Features.Conversations.Queries.GetGroupMembers;
using Comeback.Chat.Application.Features.Conversations.Queries.GetMessages;
using Comeback.Chat.Application.Features.Messages.Commands.DeleteMessageForMe;
using Comeback.Chat.Infrastructure;
using Comeback.Chat.Infrastructure.Persistence;
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

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddSignalR();

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
            // SignalR passes token as query param
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/hubs/chat"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        db.Database.Migrate();
    }

    app.UseBuildingBlocks();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { service = "chat", status = "healthy" }))
        .WithTags("Health");

    var chat = app.MapGroup("/api/chat").RequireAuthorization();

    chat.MapGet("/conversations", async (HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetConversationsQuery(userId), ct);
        return Results.Ok(result);
    });

    chat.MapPost("/conversations", async (StartConversationRequest req, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var displayName = GetDisplayName(ctx);
        var result = await sender.Send(new GetOrCreateConversationCommand(userId, displayName, req.OtherUserId, req.OtherUserDisplayName), ct);
        return Results.Ok(result);
    });

    chat.MapPost("/groups/{groupId:guid}", async (Guid groupId, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var result = await sender.Send(new GetOrCreateGroupConversationCommand(groupId, GetUserId(ctx)), ct);
        return Results.Ok(result);
    });

    chat.MapGet("/conversations/{id:guid}/messages", async (Guid id, HttpContext ctx, ISender sender, DateTime? before, int limit = 50, CancellationToken ct = default) =>
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetMessagesQuery(id, userId, before, Math.Min(limit, 100)), ct);
        return Results.Ok(result);
    });

    chat.MapDelete("/conversations/{id:guid}", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new DeleteConversationForMeCommand(id, GetUserId(ctx)), ct);
        return Results.NoContent();
    });

    chat.MapDelete("/conversations/{id:guid}/messages/{messageId:guid}", async (Guid id, Guid messageId, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new DeleteMessageForMeCommand(id, messageId, GetUserId(ctx)), ct);
        return Results.NoContent();
    });

    chat.MapGet("/conversations/{id:guid}/members", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var result = await sender.Send(new GetGroupMembersQuery(id, GetUserId(ctx)), ct);
        return Results.Ok(result);
    });

    app.MapHub<ChatHub>("/hubs/chat");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Chat service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static Guid GetUserId(HttpContext ctx) => ctx.GetUserId();

static string GetDisplayName(HttpContext ctx) => ctx.GetDisplayName();

record StartConversationRequest(Guid OtherUserId, string OtherUserDisplayName);
