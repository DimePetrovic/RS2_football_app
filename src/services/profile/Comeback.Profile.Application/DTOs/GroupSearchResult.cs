namespace Comeback.Profile.Application.DTOs;

public sealed record GroupSearchResult(Guid Id, string Name, string? AvatarUrl, int MemberCount);
