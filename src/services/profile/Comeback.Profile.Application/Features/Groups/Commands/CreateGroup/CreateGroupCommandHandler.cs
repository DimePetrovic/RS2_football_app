namespace Comeback.Profile.Application.Features.Groups.Commands.CreateGroup;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.DTOs;
using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using MediatR;
using Comeback.BuildingBlocks.Domain.Constants;

internal sealed class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, GroupSummaryResponse>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGroupCommandHandler(
        IPlayerGroupRepository groups,
        IUserProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupSummaryResponse> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var captainProfile = await _profiles.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        if (captainProfile.Role == UserRoles.Admin)
            throw new BusinessRuleException("An administrator cannot be a member of a player group.", "group.admin_forbidden");

        if (command.MemberUserIds.Count == 0)
            throw new BusinessRuleException("The group must have at least one other player.", "group.min_members");

        var memberProfiles = new List<Domain.Entities.UserProfile>();
        foreach (var userId in command.MemberUserIds.Distinct())
        {
            if (userId == command.RequestingUserId) continue;
            var profile = await _profiles.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"Player with userId {userId} not found.", "player.not_found");
            if (profile.Role == UserRoles.Admin)
                throw new BusinessRuleException("An administrator cannot be a member of a player group.", "group.admin_forbidden");
            memberProfiles.Add(profile);
        }

        if (memberProfiles.Count == 0)
            throw new BusinessRuleException("The group must have at least one other player.", "group.min_members");

        var group = PlayerGroup.Create(command.Name, command.AvatarUrl, captainProfile.Id);
        foreach (var member in memberProfiles)
            group.AddMember(member.Id, captainProfile.Id);

        _groups.Add(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GroupSummaryResponse(
            group.Id,
            group.Name,
            group.AvatarUrl,
            group.Members.Count,
            GroupMemberRole.Captain.ToString(),
            group.CreatedAt);
    }
}
