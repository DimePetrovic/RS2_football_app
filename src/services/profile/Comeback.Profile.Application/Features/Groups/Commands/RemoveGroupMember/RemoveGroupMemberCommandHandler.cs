namespace Comeback.Profile.Application.Features.Groups.Commands.RemoveGroupMember;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class RemoveGroupMemberCommandHandler : IRequestHandler<RemoveGroupMemberCommand>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveGroupMemberCommandHandler(
        IPlayerGroupRepository groups,
        IUserProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var captainProfile = await _profiles.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var memberProfile = await _profiles.GetByIdAsync(command.MemberUserId, cancellationToken)
            ?? throw new NotFoundException("Player not found.", "player.not_found");

        var group = await _groups.GetByIdWithMembersAsync(command.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        group.RemoveMember(memberProfile.Id, captainProfile.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
