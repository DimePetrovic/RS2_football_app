namespace Comeback.Match.Infrastructure.Http;

using Comeback.Match.Application.Common.Interfaces;
using System.Net.Http.Json;

public sealed class HttpPlayerRatingService : IPlayerRatingService
{
    private readonly HttpClient _http;

    public HttpPlayerRatingService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<(Guid UserId, int Rating)>> GetRatingsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var results = new List<(Guid, int)>();
        foreach (var userId in userIds)
        {
            try
            {
                var response = await _http.GetFromJsonAsync<PlayerXpDto>(
                    $"/api/rating/players/{userId}", ct);
                results.Add((userId, response?.TotalXp ?? 0));
            }
            catch
            {
                results.Add((userId, 0));
            }
        }
        return results;
    }

    private sealed record PlayerXpDto(int TotalXp);
}
