namespace Comeback.Profile.Application.Features.Groups.Commands.DeleteGroup;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand>
{
    private readonly IPlayerGroupRepository _groups;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGroupCommandHandler(
        IPlayerGroupRepository groups,
        IUserProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _groups = groups;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByIdAsync(command.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var group = await _groups.GetByIdWithMembersAsync(command.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        group.Delete(profile.Id);
        _groups.Remove(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
