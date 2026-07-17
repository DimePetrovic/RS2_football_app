namespace Comeback.Profile.Application.Features.Profiles.Queries.GetAvatarsForUsers;

using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class GetAvatarsForUsersQueryHandler : IRequestHandler<GetAvatarsForUsersQuery, List<UserAvatarDto>>
{
    private readonly IUserProfileRepository _profiles;

    public GetAvatarsForUsersQueryHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<List<UserAvatarDto>> Handle(GetAvatarsForUsersQuery query, CancellationToken cancellationToken)
    {
        var profiles = await _profiles.GetByUserIdsAsync(query.UserIds, cancellationToken);
        return profiles.Select(p => new UserAvatarDto(
            p.UserId, p.Username, p.AvatarUrl,
            string.IsNullOrWhiteSpace(p.DisplayName) ? $"{p.FirstName} {p.LastName}".Trim() : p.DisplayName,
            p.Nationality)).ToList();
    }
}
