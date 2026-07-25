namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;

public sealed class PlayerWantedConsumer : FanOutNotificationConsumer<PlayerWantedIntegrationEvent>
{
    private readonly IAllUsersClient _allUsers;

    public PlayerWantedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher,
        IAllUsersClient allUsers) : base(repository, unitOfWork, pusher)
    {
        _allUsers = allUsers;
    }

    protected override string GetNotificationType(PlayerWantedIntegrationEvent e) => "PlayerWanted";

    protected override object BuildPayload(PlayerWantedIntegrationEvent e) => new
    {
        matchId = e.MatchId,
        matchTitle = e.MatchTitle,
        organizerName = e.OrganizerDisplayName,
        position = e.Position,
    };

    protected override async Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(
        PlayerWantedIntegrationEvent e, CancellationToken ct)
    {
        // One notification to every user not already tied to the match.
        var excluded = e.ParticipantUserIds.ToHashSet();
        return (await _allUsers.GetAllUserIdsAsync(ct))
            .Where(id => !excluded.Contains(id))
            .ToArray();
    }
}
