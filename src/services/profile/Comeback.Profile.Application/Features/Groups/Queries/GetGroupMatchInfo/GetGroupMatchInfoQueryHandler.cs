namespace Comeback.Profile.Application.Features.Groups.Queries.GetGroupMatchInfo;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using Comeback.Profile.Domain.Enums;
using MediatR;

internal sealed class GetGroupMatchInfoQueryHandler : IRequestHandler<GetGroupMatchInfoQuery, GroupMatchInfoResponse>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;

    public GetGroupMatchInfoQueryHandler(IPlayerGroupRepository groups, IUserProfileRepository profiles)
    {
        _groups = groups;
        _profiles = profiles;
    }

    public async Task<GroupMatchInfoResponse> Handle(GetGroupMatchInfoQuery query, CancellationToken cancellationToken)
    {
        var group = await _groups.GetByIdWithMembersAsync(query.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        var profileIds = group.Members.Select(m => m.ProfileId);
        var profilesById = (await _profiles.GetByIdsAsync(profileIds, cancellationToken)).ToDictionary(p => p.Id);

        var members = group.Members
            .Where(m => profilesById.ContainsKey(m.ProfileId))
            .Select(m =>
            {
                var p = profilesById[m.ProfileId];
                return new GroupMemberInfo(p.UserId, p.Username);
            })
            .ToList();

        var captainMember = group.Members.FirstOrDefault(m => m.Role == GroupMemberRole.Captain)
            ?? throw new NotFoundException("The group has no captain.", "group.no_captain");
        var captainProfile = profilesById.GetValueOrDefault(captainMember.ProfileId)
            ?? throw new NotFoundException("Captain profile not found.", "profile.captain_not_found");

        return new GroupMatchInfoResponse(
            group.Id,
            group.Name,
            members,
            captainProfile.UserId,
            captainProfile.Username,
            group.AvatarUrl);
    }
}
