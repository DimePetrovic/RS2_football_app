using System.Text;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
using Comeback.Notification.Application;
using Comeback.Notification.Application.Features.Notifications.Commands.MarkAllRead;
using Comeback.Notification.Application.Features.Notifications.Commands.MarkRead;
using Comeback.Notification.Application.Features.Notifications.Queries.GetNotifications;
using Comeback.Notification.Application.Features.Notifications.Queries.GetUnreadCount;
using Comeback.Notification.Infrastructure;
using Comeback.Notification.Infrastructure.Persistence;
using Comeback.Notification.Infrastructure.Realtime;
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
            // SignalR passes token as query param
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/hubs/notifications"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        db.Database.Migrate();
    }

    app.UseBuildingBlocks();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { service = "notification", status = "healthy" }))
        .WithTags("Health");

    var notifications = app.MapGroup("/api/notifications").RequireAuthorization();

    notifications.MapGet("/", async (HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var result = await sender.Send(new GetNotificationsQuery(userId), ct);
        return Results.Ok(result);
    });

    notifications.MapGet("/unread-count", async (HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        var count = await sender.Send(new GetUnreadCountQuery(userId), ct);
        return Results.Ok(new { count });
    });

    notifications.MapPut("/{id:guid}/read", async (Guid id, HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        await sender.Send(new MarkNotificationReadCommand(id, userId), ct);
        return Results.NoContent();
    });

    notifications.MapPut("/read-all", async (HttpContext ctx, ISender sender, CancellationToken ct) =>
    {
        var userId = GetUserId(ctx);
        await sender.Send(new MarkAllNotificationsReadCommand(userId), ct);
        return Results.NoContent();
    });

    app.MapHub<NotificationHub>("/hubs/notifications");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Notification service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static Guid GetUserId(HttpContext ctx) => ctx.GetUserId();
