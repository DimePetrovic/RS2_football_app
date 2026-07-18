namespace Comeback.Chat.Api.Hubs;

using System.Linq;
using System.Security.Claims;
using Comeback.Chat.Application.Features.Conversations.Commands.MarkAsRead;
using Comeback.Chat.Application.Features.Messages.Commands.Send;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Comeback.BuildingBlocks.Infrastructure.Extensions;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly ISender _sender;

    public ChatHub(ISender sender) => _sender = sender;

    public async Task JoinConversation(Guid conversationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"conv-{conversationId}");

    public async Task SendMessage(Guid conversationId, string content)
    {
        var userId = Context.User!.GetUserId();
        var displayName = GetDisplayName();

        var result = await _sender.Send(new SendMessageCommand(conversationId, userId, displayName, content));
        var recipientIds = result.MemberUserIds.Select(id => id.ToString()).ToList();
        await Clients.Users(recipientIds).SendAsync("ReceiveMessage", result.Message);
    }

    public async Task MarkAsRead(Guid conversationId)
    {
        var userId = Context.User!.GetUserId();
        var readAt = await _sender.Send(new MarkAsReadCommand(conversationId, userId));
        await Clients.OthersInGroup($"conv-{conversationId}")
            .SendAsync("MessagesRead", conversationId, readAt);
    }

    public async Task StartTyping(Guid conversationId)
        => await Clients.OthersInGroup($"conv-{conversationId}")
            .SendAsync("UserTyping", conversationId, GetDisplayName());

    public async Task StopTyping(Guid conversationId)
        => await Clients.OthersInGroup($"conv-{conversationId}")
            .SendAsync("UserStoppedTyping", conversationId);

    private string GetDisplayName()
        => Context.User!.FindFirstValue(ClaimTypes.Name)!;
}
