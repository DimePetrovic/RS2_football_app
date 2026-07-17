namespace Comeback.Profile.Application.DTOs;

public sealed record GroupMemberInfo(Guid UserId, string DisplayName);

public sealed record GroupMatchInfoResponse(
    Guid GroupId,
    string GroupName,
    IReadOnlyList<GroupMemberInfo> Members,
    Guid CaptainUserId,
    string CaptainDisplayName,
    string? AvatarUrl);
