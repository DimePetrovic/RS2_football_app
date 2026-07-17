namespace Comeback.Profile.Application.Features.Groups.Commands.LeaveGroup;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveGroupCommandHandler(
        IPlayerGroupRepository groups,
        IUserProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LeaveGroupCommand command, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var group = await _groups.GetByIdWithMembersAsync(command.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        var shouldDelete = group.Leave(profile.Id);

        if (shouldDelete)
            _groups.Remove(group);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
