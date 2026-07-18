namespace Comeback.Match.Infrastructure.Http;

using System.Net.Http.Json;
using System.Text.Json;
using Comeback.Match.Application.Common.Interfaces;

public sealed class HttpPlayerGroupClient : IPlayerGroupClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public HttpPlayerGroupClient(HttpClient http) => _http = http;

    public async Task<GroupMatchInfo?> GetGroupMatchInfoAsync(Guid groupId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<GroupMatchInfoDto>(
                $"/api/groups/internal/{groupId}/match-info", JsonOptions, ct);
            if (response is null) return null;

            return new GroupMatchInfo(
                response.GroupId,
                response.GroupName,
                response.Members.Select(m => new GroupMemberInfo(m.UserId, m.DisplayName)).ToList(),
                response.CaptainUserId,
                response.CaptainDisplayName);
        }
        catch
        {
            return null;
        }
    }

    private sealed record GroupMemberInfoDto(Guid UserId, string DisplayName);

    private sealed record GroupMatchInfoDto(
        Guid GroupId,
        string GroupName,
        IReadOnlyList<GroupMemberInfoDto> Members,
        Guid CaptainUserId,
        string CaptainDisplayName);
}
