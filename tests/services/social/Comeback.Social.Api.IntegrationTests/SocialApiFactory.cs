namespace Comeback.Social.Api.IntegrationTests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using Comeback.Social.Domain.Entities;
using Comeback.Social.Infrastructure.Persistence;
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
/// Boots the Social API in-process against a Testcontainers Postgres database.
/// Redis (feed cache), messaging, and calls to other services are faked —
/// testira se HTTP + auth + EF + PostEnricher tok.
/// </summary>
public sealed class SocialApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Secret = "dev-secret-min-32-chars-comeback-auth-2026";

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
            Replace<IFeedCache>(services, new NoOpFeedCache());
            Replace<IMatchDetailsClient>(services, new FakeMatchDetailsClient());
            Replace<IProfileAvatarsClient>(services, new FakeProfileAvatarsClient());
            Replace<IProfileFollowersClient>(services, new FakeProfileFollowersClient());
        });
    }

    private static void Replace<TService>(IServiceCollection services, TService instance)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(instance);
    }

    public async Task SeedPostAsync(Post post, Guid? feedForUser = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
        db.Posts.Add(post);
        if (feedForUser is not null)
            db.UserFeedItems.Add(UserFeedItem.Create(feedForUser.Value, post.Id, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    public string TokenFor(Guid userId, string name = "Test Player")
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            "comeback-auth", "comeback-api",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, "Player"),
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── Fejkovi ───────────────────────────────────────────────────────────────

public sealed class NoOpFeedCache : IFeedCache
{
    public Task<List<PostResponse>?> GetAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<List<PostResponse>?>(null); // always a miss -> read from the database
    public Task SetAsync(Guid userId, List<PostResponse> posts, CancellationToken ct = default) => Task.CompletedTask;
    public Task InvalidateAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Returns fixed match details: one scorer (Home) and a goalkeeper (Away), with one goal.</summary>
public sealed class FakeMatchDetailsClient : IMatchDetailsClient
{
    public static readonly Guid ScorerUserId = Guid.NewGuid();
    public static readonly Guid KeeperUserId = Guid.NewGuid();

    public Task<MatchDetailsInfo?> GetMatchDetailsAsync(Guid matchId, CancellationToken ct = default)
        => Task.FromResult<MatchDetailsInfo?>(new MatchDetailsInfo(
            Participants:
            [
                new(Guid.NewGuid(), ScorerUserId, "Scorer", "Home", false, "Accepted"),
                new(Guid.NewGuid(), KeeperUserId, "Keeper", "Away", true, "Accepted"),
            ],
            Goals: [new(ScorerUserId, "Home", IsOwnGoal: false, AssistUserId: null)],
            Location: "Field", StartsAt: DateTime.UtcNow, GroupName: null, OpponentGroupName: null));

    public Task<List<MatchReviewInfo>> GetReviewsAsync(Guid matchId, CancellationToken ct = default)
        => Task.FromResult(new List<MatchReviewInfo>());
}

public sealed class FakeProfileAvatarsClient : IProfileAvatarsClient
{
    public Task<Dictionary<Guid, ProfileBasicInfo>> GetPlayerInfosAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
        => Task.FromResult(new Dictionary<Guid, ProfileBasicInfo>());
}

public sealed class FakeProfileFollowersClient : IProfileFollowersClient
{
    public Task<List<Guid>> GetFollowersForAnyAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
        => Task.FromResult(new List<Guid>());

    public Task<List<Guid>> GetAllUserIdsAsync(CancellationToken ct = default)
        => Task.FromResult(new List<Guid>());
}
