namespace Comeback.Profile.Application.Features.Profiles.Queries.GetAllUsers;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<AdminUserListItem>>
{
    private readonly IUserProfileRepository _profiles;

    public GetAllUsersQueryHandler(IUserProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<List<AdminUserListItem>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
    {
        var profiles = await _profiles.GetAllAsync(cancellationToken);

        return profiles
            .Where(p => p.UserId != query.ExcludeUserId)
            .Select(p => new AdminUserListItem(
                p.UserId,
                p.Username,
                p.Email,
                p.FirstName,
                p.LastName,
                p.CreatedAt))
            .ToList();
    }
}
