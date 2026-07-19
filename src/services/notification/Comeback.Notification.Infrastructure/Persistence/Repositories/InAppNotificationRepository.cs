namespace Comeback.Notification.Infrastructure.Persistence.Repositories;

using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class InAppNotificationRepository : IInAppNotificationRepository
{
    private readonly NotificationDbContext _context;

    public InAppNotificationRepository(NotificationDbContext context) => _context = context;

    public Task<List<InAppNotification>> GetByRecipientAsync(Guid recipientUserId, int limit, CancellationToken ct = default)
        => _context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct = default)
        => _context.Notifications
            .CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead, ct);

    public Task<InAppNotification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public void Add(InAppNotification notification) => _context.Notifications.Add(notification);

    public async Task MarkAllReadAsync(Guid recipientUserId, CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
}
