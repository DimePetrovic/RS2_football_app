using System.Text;
using Comeback.Profile.Application;
using Comeback.Profile.Domain;
using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using Comeback.Profile.Infrastructure;
using Comeback.Profile.Infrastructure.Persistence;
using Comeback.Profile.Api.Endpoints.Groups;
using Comeback.Profile.Api.Endpoints.Profiles;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
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

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    builder.Services.AddBuildingBlocks(
        ApplicationAssembly.Assembly,
        DomainAssembly.Assembly);

    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]!;
    var jwtAudience = builder.Configuration["JwtSettings:Audience"]!;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseBuildingBlocks();

    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapProfileEndpoints();
    app.MapGroupEndpoints();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        db.Database.Migrate();

        try
        {
            await SeedAdminProfileAsync(db);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to seed admin profile.");
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Profile service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

// Must match the admin UserId seeded in the Auth service.
static async Task SeedAdminProfileAsync(ProfileDbContext db)
{
    var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    if (await db.Profiles.AnyAsync(p => p.UserId == adminUserId))
        return;

    var profile = UserProfile.Create(
        adminUserId,
        "admin",
        "d7petrovic@gmail.com",
        "Dimitrije",
        "Petrović",
        new DateOnly(1990, 1, 1),
        Position.Midfielder,
        canPlayGoalkeeper: false,
        youthSeasons: 0,
        seniorSeasons: 0,
        role: UserRoles.Admin);

    db.Profiles.Add(profile);
    await db.SaveChangesAsync();
}

// Marker for integration tests (WebApplicationFactory<Program>).
public partial class Program;
