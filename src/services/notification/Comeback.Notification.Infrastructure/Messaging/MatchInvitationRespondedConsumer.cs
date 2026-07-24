namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchInvitationRespondedConsumer : FanOutNotificationConsumer<MatchInvitationRespondedIntegrationEvent>
{
    public MatchInvitationRespondedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchInvitationRespondedIntegrationEvent e)
        => e.Accepted ? "MatchInvitationAccepted" : "MatchInvitationDeclined";

    protected override object BuildPayload(MatchInvitationRespondedIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        responderName = e.ResponderDisplayName,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchInvitationRespondedIntegrationEvent e, CancellationToken ct) => To(e.OrganizerUserId);
}
