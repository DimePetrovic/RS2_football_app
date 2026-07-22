namespace Comeback.Chat.Application.Features.Messages.Commands.DeleteMessageForMe;
using MediatR;

public sealed record DeleteMessageForMeCommand(Guid ConversationId, Guid MessageId, Guid UserId) : IRequest;
