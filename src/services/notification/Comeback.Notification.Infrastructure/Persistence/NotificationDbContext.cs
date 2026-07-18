namespace Comeback.Notification.Infrastructure.Persistence;

using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class NotificationDbContext : DbContext, INotificationUnitOfWork
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<InAppNotification> Notifications => Set<InAppNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
}
