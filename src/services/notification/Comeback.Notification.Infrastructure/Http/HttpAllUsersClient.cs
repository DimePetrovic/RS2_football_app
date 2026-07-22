namespace Comeback.Notification.Infrastructure.Http;

using System.Net.Http.Json;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class HttpAllUsersClient : IAllUsersClient
{
    private readonly HttpClient _http;

    public HttpAllUsersClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<Guid>> GetAllUserIdsAsync(CancellationToken ct = default)
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
