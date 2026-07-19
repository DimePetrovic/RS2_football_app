namespace Comeback.Notification.Application.Features.Notifications.Queries.GetNotifications;

using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.DTOs;
using MediatR;

internal sealed class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, List<NotificationResponse>>
{
    private readonly IInAppNotificationRepository _repository;

    public GetNotificationsQueryHandler(IInAppNotificationRepository repository)
        => _repository = repository;

    public async Task<List<NotificationResponse>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var notifications = await _repository.GetByRecipientAsync(query.UserId, query.Limit, cancellationToken);
        return notifications.Select(n => new NotificationResponse(
            n.Id, n.Type, n.Payload,
            // Rows from before the type+payload re-architecture carry their text in Title/Body.
            string.IsNullOrEmpty(n.Title) ? null : n.Title,
            string.IsNullOrEmpty(n.Body) ? null : n.Body,
            n.IsRead, n.CreatedAt, n.ReadAt)).ToList();
    }
}
