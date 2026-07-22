namespace Comeback.Notification.Infrastructure.Realtime;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public sealed class NotificationHub : Hub
{
}
