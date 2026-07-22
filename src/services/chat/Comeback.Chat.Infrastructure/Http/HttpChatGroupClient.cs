namespace Comeback.Chat.Infrastructure.Http;

using System.Net.Http.Json;
using System.Text.Json;
using Comeback.Chat.Application.Common.Interfaces;

public sealed class HttpChatGroupClient : IChatGroupClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public HttpChatGroupClient(HttpClient http) => _http = http;

    public async Task<GroupChatInfo?> GetGroupInfoAsync(Guid groupId, CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<GroupMatchInfoDto>(
            $"/api/groups/internal/{groupId}/match-info", JsonOptions, ct);
        if (dto is null) return null;

        var members = dto.Members
            .Select(m => new GroupMemberInfo(m.UserId, m.DisplayName))
            .ToList();
        return new GroupChatInfo(dto.GroupId, dto.GroupName, dto.AvatarUrl, members);
    }

    private sealed record GroupMatchInfoDto(
        Guid GroupId, string GroupName, List<MemberDto> Members, Guid CaptainUserId, string CaptainDisplayName, string? AvatarUrl);
    private sealed record MemberDto(Guid UserId, string DisplayName);
}
