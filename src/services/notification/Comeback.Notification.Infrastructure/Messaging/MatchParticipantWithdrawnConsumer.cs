namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchParticipantWithdrawnConsumer : FanOutNotificationConsumer<MatchParticipantWithdrawnIntegrationEvent>
{
    public MatchParticipantWithdrawnConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchParticipantWithdrawnIntegrationEvent e) => "MatchParticipantWithdrawn";

    protected override object BuildPayload(MatchParticipantWithdrawnIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        playerName = e.WithdrawnPlayerDisplayName,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchParticipantWithdrawnIntegrationEvent e, CancellationToken ct) => To(e.OrganizerUserId);
}
