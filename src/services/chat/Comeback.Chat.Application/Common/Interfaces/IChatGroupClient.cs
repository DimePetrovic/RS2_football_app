namespace Comeback.Chat.Application.Common.Interfaces;

public sealed record GroupMemberInfo(Guid UserId, string DisplayName);
public sealed record GroupChatInfo(Guid GroupId, string Name, string? AvatarUrl, IReadOnlyList<GroupMemberInfo> Members);

public interface IChatGroupClient
{
    /// <summary>Live group roster from the Profile service (single source of truth for membership).</summary>
    Task<GroupChatInfo?> GetGroupInfoAsync(Guid groupId, CancellationToken ct = default);
}
