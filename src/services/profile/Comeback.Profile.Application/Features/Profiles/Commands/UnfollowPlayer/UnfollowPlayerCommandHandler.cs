namespace Comeback.Profile.Application.Features.Profiles.Commands.UnfollowPlayer;

using Comeback.Profile.Application.Common.Interfaces;
using MediatR;

internal sealed class UnfollowPlayerCommandHandler : IRequestHandler<UnfollowPlayerCommand>
{
    private readonly IPlayerFollowRepository _follows;
    private readonly IUnitOfWork _unitOfWork;

    public UnfollowPlayerCommandHandler(IPlayerFollowRepository follows, IUnitOfWork unitOfWork)
    {
        _follows = follows;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnfollowPlayerCommand command, CancellationToken cancellationToken)
    {
        var existing = await _follows.GetAsync(command.FollowerUserId, command.FollowedUserId, cancellationToken);
        if (existing is null) return;

        _follows.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
