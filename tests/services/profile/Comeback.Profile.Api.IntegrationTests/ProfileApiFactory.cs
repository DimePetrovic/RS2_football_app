namespace Comeback.Profile.Api.IntegrationTests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Domain.Events;
using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using Comeback.Profile.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Boots the Profile API in-process against a Testcontainers Postgres database.
/// Messaging is faked; exercises the HTTP + auth + EF + domain flow.
/// </summary>
public sealed class ProfileApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Secret = "dev-secret-min-32-chars-comeback-auth-2026";
    private const string Issuer = "comeback-auth";
    private const string Audience = "comeback-api";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
        });
    }

    /// <summary>Inserts a profile directly into the database (profiles otherwise originate from an auth event).</summary>
    public async Task SeedProfileAsync(Guid userId, string username, string role = "Player")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        db.Profiles.Add(UserProfile.Create(
            userId, username, $"{username}@test.com", "First", "Last",
            new DateOnly(1995, 1, 1), Position.Midfielder,
            canPlayGoalkeeper: false, youthSeasons: 0, seniorSeasons: 0, role));
        await db.SaveChangesAsync();
    }

    public string TokenFor(Guid userId, string name = "Test Player", string role = "Player")
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            Issuer, Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role),
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class NoOpIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : class, IIntegrationEvent => Task.CompletedTask;
}
