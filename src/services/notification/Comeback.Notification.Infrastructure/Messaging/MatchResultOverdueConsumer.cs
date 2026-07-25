namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchResultOverdueConsumer : FanOutNotificationConsumer<MatchResultOverdueIntegrationEvent>
{
    public MatchResultOverdueConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchResultOverdueIntegrationEvent e) => "MatchResultOverdue";

    protected override object BuildPayload(MatchResultOverdueIntegrationEvent e)
        => new { matchId = e.MatchId, matchTitle = e.MatchTitle };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchResultOverdueIntegrationEvent e, CancellationToken ct) => To(e.OrganizerUserId);
}
