namespace Comeback.Notification.Application.Common.Interfaces;

public interface INotificationUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
