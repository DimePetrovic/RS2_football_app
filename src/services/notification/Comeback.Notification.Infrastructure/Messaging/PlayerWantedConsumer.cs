namespace Comeback.Notification.Infrastructure.Messaging;

using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using MassTransit;

public sealed class PlayerWantedConsumer : IConsumer<PlayerWantedIntegrationEvent>
{
    private readonly IInAppNotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationPusher _pusher;
    private readonly IAllUsersClient _allUsers;

    public PlayerWantedConsumer(
        IInAppNotificationRepository repository,
        INotificationUnitOfWork unitOfWork,
        INotificationPusher pusher,
        IAllUsersClient allUsers)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _pusher = pusher;
        _allUsers = allUsers;
    }

    public async Task Consume(ConsumeContext<PlayerWantedIntegrationEvent> context)
    {
        var e = context.Message;
        var ct = context.CancellationToken;

        // One notification to every user not already tied to the match.
        var excluded = e.ParticipantUserIds.ToHashSet();
        var recipients = (await _allUsers.GetAllUserIdsAsync(ct))
            .Where(id => !excluded.Contains(id))
            .ToList();
        if (recipients.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            matchId = e.MatchId,
            matchTitle = e.MatchTitle,
            organizerName = e.OrganizerDisplayName,
            position = e.Position,
        });

        var notifications = recipients
            .Select(userId => new InAppNotification(userId, "PlayerWanted", payload))
            .ToList();

        foreach (var notification in notifications)
            _repository.Add(notification);

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var notification in notifications)
            await _pusher.PushAsync(notification, ct);
    }
}
