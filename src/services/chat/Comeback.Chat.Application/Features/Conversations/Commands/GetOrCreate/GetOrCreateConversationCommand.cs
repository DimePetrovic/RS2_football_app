namespace Comeback.Chat.Application.Features.Conversations.Commands.GetOrCreate;
using Comeback.Chat.Application.DTOs;
using MediatR;

public sealed record GetOrCreateConversationCommand(
    Guid RequestingUserId,
    string RequestingUserDisplayName,
    Guid OtherUserId,
    string OtherUserDisplayName
) : IRequest<ConversationSummaryDto>;
