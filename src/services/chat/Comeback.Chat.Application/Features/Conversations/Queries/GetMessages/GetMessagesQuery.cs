namespace Comeback.Chat.Application.Features.Conversations.Queries.GetMessages;
using Comeback.Chat.Application.DTOs;
using MediatR;

public sealed record GetMessagesQuery(Guid ConversationId, Guid UserId, DateTime? Before, int Limit = 50) : IRequest<List<MessageDto>>;
