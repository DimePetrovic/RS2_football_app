namespace Comeback.Chat.Application.Features.Conversations.Commands.MarkAsRead;
using Comeback.BuildingBlocks.Application.Messaging;

public sealed record MarkAsReadCommand(Guid ConversationId, Guid UserId) : ICommand<DateTime>;
