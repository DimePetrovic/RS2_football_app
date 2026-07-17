namespace Comeback.Profile.Application.Features.Profiles.Queries.GetAvatarsForUsers;

using MediatR;

public sealed record UserAvatarDto(Guid UserId, string Username, string? AvatarUrl, string DisplayName, string? Nationality);

public sealed record GetAvatarsForUsersQuery(IReadOnlyList<Guid> UserIds) : IRequest<List<UserAvatarDto>>;
