namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class GroupMatchInviteRespondedConsumer : FanOutNotificationConsumer<GroupMatchInviteRespondedIntegrationEvent>
{
    public GroupMatchInviteRespondedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(GroupMatchInviteRespondedIntegrationEvent e)
        => e.Accepted ? "GroupMatchInviteAccepted" : "GroupMatchInviteDeclined";

    protected override object BuildPayload(GroupMatchInviteRespondedIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        opponentGroupName = e.OpponentGroupName,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        GroupMatchInviteRespondedIntegrationEvent e, CancellationToken ct) => To(e.OrganizerUserId);
}
