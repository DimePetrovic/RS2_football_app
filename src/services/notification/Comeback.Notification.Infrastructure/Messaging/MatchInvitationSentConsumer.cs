namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class MatchInvitationSentConsumer : FanOutNotificationConsumer<MatchInvitationSentIntegrationEvent>
{
    public MatchInvitationSentConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(MatchInvitationSentIntegrationEvent e) => "MatchInvitation";

    protected override object BuildPayload(MatchInvitationSentIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        organizerName = e.OrganizerDisplayName,
        location = e.Location,
        startsAt = e.StartsAt,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        MatchInvitationSentIntegrationEvent e, CancellationToken ct) => To(e.InviteeUserId);
}
