namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class PlayerJoinedViaPublicCallConsumer : FanOutNotificationConsumer<PlayerJoinedViaPublicCallIntegrationEvent>
{
    public PlayerJoinedViaPublicCallConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher) : base(repository, unitOfWork, pusher) { }

    protected override string GetNotificationType(PlayerJoinedViaPublicCallIntegrationEvent e) => "PlayerJoinedViaPublicCall";

    protected override object BuildPayload(PlayerJoinedViaPublicCallIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        playerName = e.PlayerDisplayName,
    };

    protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        PlayerJoinedViaPublicCallIntegrationEvent e, CancellationToken ct) => To(e.OrganizerUserId);
}
