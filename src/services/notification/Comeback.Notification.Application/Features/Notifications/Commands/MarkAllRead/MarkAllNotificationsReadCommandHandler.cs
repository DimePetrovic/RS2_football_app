namespace Comeback.Notification.Application.Features.Notifications.Commands.MarkAllRead;

using Comeback.Notification.Application.Common.Interfaces;
using MediatR;

internal sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public MarkAllNotificationsReadCommandHandler(IInAppNotificationRepository repository, INotificationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        await _repository.MarkAllReadAsync(command.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
