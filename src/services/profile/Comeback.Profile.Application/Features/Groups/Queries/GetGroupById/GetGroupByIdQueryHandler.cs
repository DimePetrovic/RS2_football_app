namespace Comeback.Profile.Application.Features.Groups.Queries.GetGroupById;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using MediatR;

internal sealed class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, GroupDetailResponse>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;

    public GetGroupByIdQueryHandler(IPlayerGroupRepository groups, IUserProfileRepository profiles)
    {
        _groups = groups;
        _profiles = profiles;
    }

    public async Task<GroupDetailResponse> Handle(GetGroupByIdQuery query, CancellationToken cancellationToken)
    {
        var requestingProfile = await _profiles.GetByIdAsync(query.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var group = await _groups.GetByIdWithMembersAsync(query.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        var requestingMember = group.Members.FirstOrDefault(m => m.ProfileId == requestingProfile.Id)
            ?? throw new ForbiddenException("You are not a member of this group.", "group.not_member");

        var profileIds = group.Members.Select(m => m.ProfileId);
        var memberProfiles = (await _profiles.GetByIdsAsync(profileIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var members = group.Members.Select(m =>
        {
            memberProfiles.TryGetValue(m.ProfileId, out var p);
            return new GroupMemberResponse(
                m.ProfileId,
                p?.UserId ?? Guid.Empty,
                p?.Username ?? string.Empty,
                p?.FirstName ?? string.Empty,
                p?.LastName ?? string.Empty,
                p?.DisplayName,
                p?.AvatarUrl,
                m.Role.ToString(),
                m.JoinedAt);
        }).ToList();

        return new GroupDetailResponse(
            group.Id,
            group.Name,
            group.AvatarUrl,
            members,
            requestingMember.Role.ToString(),
            group.CreatedAt,
            group.UpdatedAt);
    }
}
