namespace Comeback.Notification.Application.Common.Interfaces;

using Comeback.Notification.Application.Entities;

public interface INotificationPusher
{
    Task PushAsync(InAppNotification notification, CancellationToken ct = default);
}
