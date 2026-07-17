namespace Comeback.Profile.Application.Features.Groups.Queries.GetMyGroups;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using Comeback.Profile.Domain.Enums;
using MediatR;

internal sealed class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, List<GroupSummaryResponse>>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;

    public GetMyGroupsQueryHandler(IPlayerGroupRepository groups, IUserProfileRepository profiles)
    {
        _groups = groups;
        _profiles = profiles;
    }

    public async Task<List<GroupSummaryResponse>> Handle(GetMyGroupsQuery query, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByIdAsync(query.UserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var groups = await _groups.GetByMemberProfileIdAsync(profile.Id, cancellationToken);

        return groups.Select(g =>
        {
            var member = g.Members.First(m => m.ProfileId == profile.Id);
            return new GroupSummaryResponse(
                g.Id,
                g.Name,
                g.AvatarUrl,
                g.Members.Count,
                member.Role.ToString(),
                g.CreatedAt);
        }).ToList();
    }
}
