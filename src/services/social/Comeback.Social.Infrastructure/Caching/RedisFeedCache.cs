namespace Comeback.Social.Infrastructure.Caching;

using System.Text.Json;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using StackExchange.Redis;

public sealed class RedisFeedCache : IFeedCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private readonly IConnectionMultiplexer _redis;

    public RedisFeedCache(IConnectionMultiplexer redis) => _redis = redis;

    private static string Key(Guid userId) => $"feed:{userId}";

    public async Task<List<PostResponse>?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(Key(userId));
            if (value.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<List<PostResponse>>((string)value!);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(Guid userId, List<PostResponse> posts, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(posts);
            await db.StringSetAsync(Key(userId), json, Ttl);
        }
        catch
        {
            // Cache is a performance optimization, not a source of truth — ignore failures.
        }
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(Key(userId));
        }
        catch
        {
            // Best-effort invalidation; a stale cache entry self-expires via TTL regardless.
        }
    }
}
