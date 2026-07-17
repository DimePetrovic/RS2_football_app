namespace Comeback.Profile.Application.DTOs;

public sealed record GroupMemberResponse(
    Guid ProfileId,
    Guid UserId,
    string Username,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);

public sealed record GroupSummaryResponse(
    Guid Id,
    string Name,
    string? AvatarUrl,
    int MemberCount,
    string MyRole,
    DateTime CreatedAt);

public sealed record GroupDetailResponse(
    Guid Id,
    string Name,
    string? AvatarUrl,
    IReadOnlyList<GroupMemberResponse> Members,
    string MyRole,
    DateTime CreatedAt,
    DateTime UpdatedAt);
