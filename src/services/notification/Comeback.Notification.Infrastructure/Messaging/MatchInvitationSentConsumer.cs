namespace Comeback.Notification.Infrastructure.Messaging;

using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using MassTransit;

public sealed class MatchInvitationSentConsumer : IConsumer<MatchInvitationSentIntegrationEvent>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;

    public MatchInvitationSentConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _pusher = pusher;
    }

    public async Task Consume(ConsumeContext<MatchInvitationSentIntegrationEvent> context)
    {
        var e = context.Message;
        var payload = JsonSerializer.Serialize(new
        {
            matchId = e.MatchId,
            organizerName = e.OrganizerDisplayName,
            location = e.Location,
            startsAt = e.StartsAt,
        });

        var notification = new InAppNotification(
            recipientUserId: e.InviteeUserId,
            type: "MatchInvitation",
            payload: payload);

        _repository.Add(notification);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        await _pusher.PushAsync(notification, context.CancellationToken);
    }
}
