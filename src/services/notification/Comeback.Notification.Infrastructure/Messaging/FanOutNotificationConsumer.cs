namespace Comeback.Notification.Infrastructure.Messaging;

using System.Text.Json;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using MassTransit;

/// <summary>
/// Base class for the in-app notification consumers. Collapses the identical
/// "resolve recipients → persist a notification per recipient → save → push" fan-out that every
/// concrete consumer used to repeat verbatim. A concrete consumer only supplies the notification
/// <see cref="GetNotificationType"/>, the <see cref="BuildPayload"/> projection, and the
/// <see cref="GetRecipientsAsync"/> audience.
/// </summary>
public abstract class FanOutNotificationConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;

    protected FanOutNotificationConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _pusher = pusher;
    }

    /// <summary>The notification type string; may depend on the event (e.g. accepted vs declined).</summary>
    protected abstract string GetNotificationType(TEvent e);

    /// <summary>The per-notification payload object, serialized to JSON.</summary>
    protected abstract object BuildPayload(TEvent e);

    /// <summary>The recipients of the notification.</summary>
    protected abstract Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(TEvent e, CancellationToken ct);

    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var e = context.Message;
        var recipients = await GetRecipientsAsync(e, context.CancellationToken);
        if (recipients.Count == 0) return;

        var type = GetNotificationType(e);
        var payload = JsonSerializer.Serialize(BuildPayload(e));

        var notifications = recipients
            .Select(userId => new InAppNotification(userId, type, payload))
            .ToList();

        foreach (var notification in notifications)
            _repository.Add(notification);

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);

        foreach (var notification in notifications)
            await _pusher.PushAsync(notification, context.CancellationToken);
    }

    /// <summary>Convenience for a single-recipient notification.</summary>
    protected static Task<IReadOnlyCollection<Guid>> To(Guid recipient)
        => Task.FromResult<IReadOnlyCollection<Guid>>(new[] { recipient });

    /// <summary>Convenience for a multi-recipient notification.</summary>
    protected static Task<IReadOnlyCollection<Guid>> To(IEnumerable<Guid> recipients)
        => Task.FromResult<IReadOnlyCollection<Guid>>(recipients.ToArray());
}
