namespace Comeback.Social.Infrastructure.Http;

using System.Net.Http.Json;
using Comeback.Social.Application.Common.Interfaces;

public sealed class HttpProfileAvatarsClient : IProfileAvatarsClient
{
    private readonly HttpClient _http;

    public HttpProfileAvatarsClient(HttpClient http) => _http = http;

    public async Task<Dictionary<Guid, ProfileBasicInfo>> GetPlayerInfosAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return [];

        try
        {
            var query = string.Join(',', idList);
            var result = await _http.GetFromJsonAsync<List<UserAvatarDto>>(
                $"/api/profiles/internal/avatars?userIds={query}", ct);
            return (result ?? [])
                .ToDictionary(a => a.UserId, a => new ProfileBasicInfo(a.Username, a.AvatarUrl, a.DisplayName, a.Nationality));
        }
        catch
        {
            // Profile service unavailable — the post is shown without avatars (initials as fallback).
            return [];
        }
    }

    private sealed record UserAvatarDto(Guid UserId, string Username, string? AvatarUrl, string? DisplayName, string? Nationality);
}
