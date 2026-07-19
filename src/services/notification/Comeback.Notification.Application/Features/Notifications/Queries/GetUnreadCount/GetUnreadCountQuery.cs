namespace Comeback.Notification.Application.Features.Notifications.Queries.GetUnreadCount;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record GetUnreadCountQuery(Guid UserId) : IQuery<int>;
