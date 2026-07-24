namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class GroupMatchInviteConsumer : FanOutNotificationConsumer<GroupMatchInviteIntegrationEvent>
{
    public GroupMatchInviteConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(GroupMatchInviteIntegrationEvent e) => "GroupMatchInvite";

    protected override object BuildPayload(GroupMatchInviteIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        organizerName = e.OrganizerDisplayName,
        organizerGroupName = e.OrganizerGroupName,
        location = e.Location,
        startsAt = e.StartsAt,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        GroupMatchInviteIntegrationEvent e, CancellationToken ct) => To(e.CaptainUserId);
}
