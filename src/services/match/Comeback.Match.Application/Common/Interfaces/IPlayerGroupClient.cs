namespace Comeback.Match.Application.Common.Interfaces;

public sealed record GroupMemberInfo(Guid UserId, string DisplayName);

public sealed record GroupMatchInfo(
    Guid GroupId,
    string GroupName,
    IReadOnlyList<GroupMemberInfo> Members,
    Guid CaptainUserId,
    string CaptainDisplayName);

public interface IPlayerGroupClient
{
    Task<GroupMatchInfo?> GetGroupMatchInfoAsync(Guid groupId, CancellationToken ct = default);
}
