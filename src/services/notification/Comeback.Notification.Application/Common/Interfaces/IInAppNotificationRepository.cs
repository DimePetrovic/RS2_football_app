namespace Comeback.Notification.Application.Common.Interfaces;

using Comeback.Notification.Application.Entities;

public interface IInAppNotificationRepository
{
    Task<List<InAppNotification>> GetByRecipientAsync(Guid recipientUserId, int limit, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct = default);
    Task<InAppNotification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(InAppNotification notification);
    Task MarkAllReadAsync(Guid recipientUserId, CancellationToken ct = default);
}
