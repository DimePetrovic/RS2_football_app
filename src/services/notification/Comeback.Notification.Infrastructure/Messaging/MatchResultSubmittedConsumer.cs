namespace Comeback.Notification.Infrastructure.Messaging;

using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using MassTransit;

public sealed class MatchResultSubmittedConsumer : IConsumer<MatchResultSubmittedIntegrationEvent>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;

    public MatchResultSubmittedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _pusher = pusher;
    }

    public async Task Consume(ConsumeContext<MatchResultSubmittedIntegrationEvent> context)
    {
        var e = context.Message;
        var payload = JsonSerializer.Serialize(new
        {
            matchId = e.MatchId,
            matchTitle = e.MatchTitle,
            homeScore = e.HomeScore,
            awayScore = e.AwayScore,
        });

        var notifications = e.NotifyUserIds.Select(userId => new InAppNotification(
            recipientUserId: userId,
            type: "MatchResultSubmitted",
            payload: payload)).ToList();

        foreach (var notification in notifications)
            _repository.Add(notification);

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);

        foreach (var notification in notifications)
            await _pusher.PushAsync(notification, context.CancellationToken);
    }
}
