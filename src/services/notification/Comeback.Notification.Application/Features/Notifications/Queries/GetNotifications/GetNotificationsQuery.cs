namespace Comeback.Notification.Application.Features.Notifications.Queries.GetNotifications;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Notification.Application.DTOs;

public sealed record GetNotificationsQuery(Guid UserId, int Limit = 50) : IQuery<List<NotificationResponse>>;
