namespace Comeback.Notification.Infrastructure.Messaging;

using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using MassTransit;

public sealed class MatchInvitationRespondedConsumer : IConsumer<MatchInvitationRespondedIntegrationEvent>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;

    public MatchInvitationRespondedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _pusher = pusher;
    }

    public async Task Consume(ConsumeContext<MatchInvitationRespondedIntegrationEvent> context)
    {
        var e = context.Message;
        var payload = JsonSerializer.Serialize(new
        {
            matchId = e.MatchId,
            matchTitle = e.MatchTitle,
            responderName = e.ResponderDisplayName,
        });

        var notification = new InAppNotification(
            recipientUserId: e.OrganizerUserId,
            type: e.Accepted ? "MatchInvitationAccepted" : "MatchInvitationDeclined",
            payload: payload);

        _repository.Add(notification);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        await _pusher.PushAsync(notification, context.CancellationToken);
    }
}
