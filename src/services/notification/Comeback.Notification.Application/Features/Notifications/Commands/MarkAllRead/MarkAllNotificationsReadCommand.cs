namespace Comeback.Notification.Application.Features.Notifications.Commands.MarkAllRead;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : ICommand;
