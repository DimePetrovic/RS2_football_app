namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchMissedConsumer : FanOutNotificationConsumer<MatchMissedIntegrationEvent>
{
    public MatchMissedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchMissedIntegrationEvent e) => "MatchMissed";

    protected override object BuildPayload(MatchMissedIntegrationEvent e)
        => new { matchId = e.MatchId, matchTitle = e.MatchTitle };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchMissedIntegrationEvent e, CancellationToken ct) => To(e.NotifyUserIds);
}
