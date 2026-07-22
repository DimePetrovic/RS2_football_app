namespace Comeback.Notification.Infrastructure.Realtime;

using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.DTOs;
using Comeback.Notification.Application.Entities;
using Microsoft.AspNetCore.SignalR;

public sealed class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushAsync(InAppNotification notification, CancellationToken ct = default)
    {
        var dto = new NotificationResponse(
            notification.Id,
            notification.Type,
            notification.Payload,
            null,
            null,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);

        return _hubContext.Clients
            .User(notification.RecipientUserId.ToString())
            .SendAsync("NotificationReceived", dto, ct);
    }
}
