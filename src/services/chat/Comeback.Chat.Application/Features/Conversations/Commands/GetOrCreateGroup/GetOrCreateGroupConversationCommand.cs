namespace Comeback.Chat.Application.Features.Conversations.Commands.GetOrCreateGroup;
using Comeback.Chat.Application.DTOs;
using MediatR;

public sealed record GetOrCreateGroupConversationCommand(Guid GroupId, Guid RequesterUserId)
    : IRequest<ConversationSummaryDto>;
