namespace Comeback.Profile.Application.Features.Profiles.Commands.FollowPlayer;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Domain.Entities;
using MediatR;

internal sealed class FollowPlayerCommandHandler : IRequestHandler<FollowPlayerCommand>
{
    private readonly IPlayerFollowRepository _follows;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public FollowPlayerCommandHandler(
        IPlayerFollowRepository follows, IUserProfileRepository profiles, IUnitOfWork unitOfWork)
    {
        _follows = follows;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(FollowPlayerCommand command, CancellationToken cancellationToken)
    {
        if (command.FollowerUserId == command.FollowedUserId)
            throw new BusinessRuleException("You cannot follow your own profile.", "follow.self");

        var target = await _profiles.GetByIdAsync(command.FollowedUserId, cancellationToken)
            ?? throw new NotFoundException("Profile not found.", "profile.not_found");

        var existing = await _follows.GetAsync(command.FollowerUserId, command.FollowedUserId, cancellationToken);
        if (existing is not null) return;

        _follows.Add(PlayerFollow.Create(command.FollowerUserId, command.FollowedUserId));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
