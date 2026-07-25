namespace Comeback.BuildingBlocks.Infrastructure.Http;

using System.Net.Http.Json;
using System.Text.Json;
using Comeback.BuildingBlocks.Application.Clients;

/// <summary>
/// Shared <see cref="IPlayerInfoClient"/> over the Profile service's internal avatars endpoint.
/// Register with <c>AddHttpClient&lt;IPlayerInfoClient, HttpPlayerInfoClient&gt;(...)</c> and set the
/// client's <c>BaseAddress</c> to the Profile API. When the Profile service is unavailable the call
/// degrades to an empty list so callers fall back to the display name without an avatar.
/// </summary>
public sealed class HttpPlayerInfoClient : IPlayerInfoClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public HttpPlayerInfoClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<PlayerInfo>> GetPlayerInfosAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return [];

        try
        {
            var result = await _http.GetFromJsonAsync<List<UserAvatarDto>>(
                $"/api/profiles/internal/avatars?userIds={string.Join(',', idList)}", JsonOptions, ct);
            return (result ?? [])
                .Select(a => new PlayerInfo(a.UserId, a.Username, a.AvatarUrl, a.DisplayName, a.Nationality))
                .ToList();
        }
        catch
        {
            // Profile service unavailable — degrade to the display name without an avatar.
            return [];
        }
    }

    private sealed record UserAvatarDto(Guid UserId, string Username, string? AvatarUrl, string? DisplayName, string? Nationality);
}
