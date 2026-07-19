namespace Comeback.Notification.Infrastructure.Messaging;

using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using MassTransit;

public sealed class MatchResultReminderConsumer : IConsumer<MatchResultReminderIntegrationEvent>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;

    public MatchResultReminderConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _pusher = pusher;
    }

    public async Task Consume(ConsumeContext<MatchResultReminderIntegrationEvent> context)
    {
        var e = context.Message;
        var notification = new InAppNotification(
            recipientUserId: e.OrganizerUserId,
            type: "MatchResultReminder",
            payload: JsonSerializer.Serialize(new { matchId = e.MatchId, matchTitle = e.MatchTitle }));

        _repository.Add(notification);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        await _pusher.PushAsync(notification, context.CancellationToken);
    }
}
