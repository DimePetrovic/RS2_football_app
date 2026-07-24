namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchResultReminderConsumer : FanOutNotificationConsumer<MatchResultReminderIntegrationEvent>
{
    public MatchResultReminderConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchResultReminderIntegrationEvent e) => "MatchResultReminder";

    protected override object BuildPayload(MatchResultReminderIntegrationEvent e)
        => new { matchId = e.MatchId, matchTitle = e.MatchTitle };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchResultReminderIntegrationEvent e, CancellationToken ct) => To(e.OrganizerUserId);
}
