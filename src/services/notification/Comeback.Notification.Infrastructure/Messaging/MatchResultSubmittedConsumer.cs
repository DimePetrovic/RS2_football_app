namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchResultSubmittedConsumer : FanOutNotificationConsumer<MatchResultSubmittedIntegrationEvent>
{
    public MatchResultSubmittedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchResultSubmittedIntegrationEvent e) => "MatchResultSubmitted";

    protected override object BuildPayload(MatchResultSubmittedIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        homeScore = e.HomeScore,
        awayScore = e.AwayScore,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchResultSubmittedIntegrationEvent e, CancellationToken ct) => To(e.NotifyUserIds);
}
