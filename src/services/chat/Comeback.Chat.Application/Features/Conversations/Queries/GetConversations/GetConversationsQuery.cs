namespace Comeback.Chat.Application.Features.Conversations.Queries.GetConversations;
using Comeback.Chat.Application.DTOs;
using MediatR;

public sealed record GetConversationsQuery(Guid UserId) : IRequest<List<ConversationSummaryDto>>;
