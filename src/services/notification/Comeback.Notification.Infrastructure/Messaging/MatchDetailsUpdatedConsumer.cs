namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchDetailsUpdatedConsumer : FanOutNotificationConsumer<MatchDetailsUpdatedIntegrationEvent>
{
    public MatchDetailsUpdatedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchDetailsUpdatedIntegrationEvent e) => "MatchDetailsUpdated";

    protected override object BuildPayload(MatchDetailsUpdatedIntegrationEvent e)
        => new { matchId = e.MatchId, matchTitle = e.MatchTitle };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchDetailsUpdatedIntegrationEvent e, CancellationToken ct) => To(e.NotifyUserIds);
}
