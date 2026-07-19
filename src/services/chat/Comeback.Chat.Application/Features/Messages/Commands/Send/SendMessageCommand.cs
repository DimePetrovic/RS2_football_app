namespace Comeback.Chat.Application.Features.Messages.Commands.Send;
using Comeback.Chat.Application.DTOs;
using MediatR;

public sealed record SendMessageCommand(
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Content
) : IRequest<SendMessageResult>;

public sealed record SendMessageResult(MessageDto Message, IReadOnlyList<Guid> MemberUserIds);
