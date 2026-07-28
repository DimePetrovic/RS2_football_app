using Comeback.Auth.Api.Endpoints.Auth;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.Auth.Infrastructure;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

using ApplicationAssembly = Comeback.Auth.Application.AssemblyReference;
using DomainAssembly = Comeback.Auth.Domain.AssemblyReference;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

    builder.Services
        .AddBuildingBlocks(ApplicationAssembly.Assembly, DomainAssembly.Assembly)
        .AddInfrastructure(builder.Configuration);

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

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseBuildingBlocks();

    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { service = "auth", status = "healthy" }))
        .WithTags("Health")
        .WithOpenApi();

    app.MapAuthEndpoints();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Comeback.Auth.Infrastructure.Persistence.AuthDbContext>();
        db.Database.Migrate();

        try
        {
            await SeedAdminUserAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to seed admin user.");
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Auth service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Must match the admin UserId seeded in the Profile service.
static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    const string adminEmail = "admin@comeback.com";

    if (await userManager.FindByEmailAsync(adminEmail) is not null)
        return;

    var admin = new ApplicationUser
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Email = adminEmail,
        UserName = "admin",
        EmailConfirmed = true,
        Role = UserRole.Admin,
        AccountStatus = AccountStatus.Active,
        CreatedAt = DateTime.UtcNow,
    };

    var result = await userManager.CreateAsync(admin, "Test!234");

    if (!result.Succeeded)
        Log.Warning("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
}

// Marker for integration tests (WebApplicationFactory<Program>).
public partial class Program;
