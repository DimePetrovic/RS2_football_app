namespace Comeback.Chat.Application.Features.Conversations.Commands.DeleteConversationForMe;
using MediatR;

public sealed record DeleteConversationForMeCommand(Guid ConversationId, Guid UserId) : IRequest;
