namespace Comeback.Social.Infrastructure.Http;

using System.Net.Http.Json;
using Comeback.Social.Application.Common.Interfaces;

public sealed class HttpProfileFollowersClient : IProfileFollowersClient
{
    private readonly HttpClient _http;

    public HttpProfileFollowersClient(HttpClient http) => _http = http;

    public async Task<List<Guid>> GetFollowersForAnyAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var idList = userIds.ToList();
        if (idList.Count == 0) return [];

        try
        {
            var query = string.Join(',', idList);
            var result = await _http.GetFromJsonAsync<List<Guid>>(
                $"/api/profiles/internal/followers-for-any?userIds={query}", ct);
            return result ?? [];
        }
        catch
        {
            // Profile service unreachable — fall back to "no extra followers"; participants themselves still see the post.
            return [];
        }
    }

    public async Task<List<Guid>> GetAllUserIdsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<Guid>>("/api/profiles/internal/all-ids", ct);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }
}
