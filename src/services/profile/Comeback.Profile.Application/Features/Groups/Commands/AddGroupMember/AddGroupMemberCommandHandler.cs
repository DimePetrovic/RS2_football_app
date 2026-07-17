namespace Comeback.Profile.Application.Features.Groups.Commands.AddGroupMember;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using MediatR;
using Comeback.BuildingBlocks.Domain.Constants;

internal sealed class AddGroupMemberCommandHandler : IRequestHandler<AddGroupMemberCommand>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public AddGroupMemberCommandHandler(
        IPlayerGroupRepository groups,
        IUserProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var captainProfile = await _profiles.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var memberProfile = await _profiles.GetByIdAsync(command.MemberUserId, cancellationToken)
            ?? throw new NotFoundException("Player not found.", "player.not_found");

        if (memberProfile.Role == UserRoles.Admin)
            throw new BusinessRuleException("An administrator cannot be a member of a player group.", "group.admin_forbidden");

        var group = await _groups.GetByIdWithMembersAsync(command.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        group.AddMember(memberProfile.Id, captainProfile.Id);

        var newMember = group.Members.First(m => m.ProfileId == memberProfile.Id);
        _groups.TrackMember(newMember);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
