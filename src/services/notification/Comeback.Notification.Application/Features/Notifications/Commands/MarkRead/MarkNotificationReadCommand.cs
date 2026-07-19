namespace Comeback.Notification.Application.Features.Notifications.Commands.MarkRead;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : ICommand;
