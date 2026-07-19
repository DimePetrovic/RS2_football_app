namespace Comeback.Match.Api.IntegrationTests;

using Comeback.BuildingBlocks.Domain.Events;
using Comeback.Match.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Boots the Match API in-process against a real Postgres database (Testcontainers).
/// Messaging (RabbitMQ) and calls to other services are replaced with fakes —
/// exercises HTTP + auth + EF + domain flow, not cross-service infrastructure.
/// </summary>
public sealed class MatchApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public FakeMatchEventPublisher Events { get; } = new();
    public FakeMatchJobScheduler Scheduler { get; } = new();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            // The MassTransit bus is not started in tests (no RabbitMQ).
            services.RemoveAll<IHostedService>();

            Replace<IMatchEventPublisher>(services, Events);
            Replace<IPlayerInfoClient>(services, new FakePlayerInfoClient());
            Replace<IPlayerGroupClient>(services, new FakePlayerGroupClient());
            Replace<IPlayerRatingService>(services, new FakePlayerRatingService());
            Replace<IMatchJobScheduler>(services, Scheduler);
        });
    }

    private static void Replace<TService>(IServiceCollection services, TService instance)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(instance);
    }
}

// ── Fejkovi za zavisnosti van Match servisa ──────────────────────────────

public sealed class FakeMatchEventPublisher : IMatchEventPublisher
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent
    {
        Published.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

public sealed class FakePlayerInfoClient : IPlayerInfoClient
{
    public Task<IReadOnlyList<PlayerInfo>> GetPlayerInfosAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PlayerInfo>>([]);
}

public sealed class FakePlayerGroupClient : IPlayerGroupClient
{
    public Task<GroupMatchInfo?> GetGroupMatchInfoAsync(Guid groupId, CancellationToken ct = default)
        => Task.FromResult<GroupMatchInfo?>(null);
}

public sealed class FakePlayerRatingService : IPlayerRatingService
{
    public Task<IReadOnlyList<(Guid UserId, int Rating)>> GetRatingsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<(Guid, int)>>([]);
}

public sealed class FakeMatchJobScheduler : IMatchJobScheduler
{
    public List<(Guid MatchId, DateTimeOffset RunAt)> Scheduled { get; } = [];
    public List<string> Cancelled { get; } = [];

    public string ScheduleResultReminder(Guid matchId, DateTimeOffset runAt)
    {
        Scheduled.Add((matchId, runAt));
        return $"job-{Scheduled.Count}";
    }

    public void CancelJob(string? jobId)
    {
        if (!string.IsNullOrEmpty(jobId)) Cancelled.Add(jobId);
    }
}
