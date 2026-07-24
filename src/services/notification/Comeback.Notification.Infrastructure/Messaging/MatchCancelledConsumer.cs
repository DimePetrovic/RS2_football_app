namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchCancelledConsumer : FanOutNotificationConsumer<MatchCancelledIntegrationEvent>
{
    public MatchCancelledConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchCancelledIntegrationEvent e) => "MatchCancelled";

    protected override object BuildPayload(MatchCancelledIntegrationEvent e)
        => new { matchId = e.MatchId, matchTitle = e.MatchTitle };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchCancelledIntegrationEvent e, CancellationToken ct) => To(e.NotifyUserIds);
}
