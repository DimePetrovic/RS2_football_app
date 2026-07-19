namespace Comeback.Notification.Application.Features.Notifications.Commands.MarkRead;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Notification.Application.Common.Interfaces;
using MediatR;

internal sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public MarkNotificationReadCommandHandler(IInAppNotificationRepository repository, INotificationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(command.NotificationId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.", "notification.not_found");

        if (notification.RecipientUserId != command.UserId)
            throw new ForbiddenException("You do not have access to this notification.", "notification.access_forbidden");

        notification.MarkAsRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
