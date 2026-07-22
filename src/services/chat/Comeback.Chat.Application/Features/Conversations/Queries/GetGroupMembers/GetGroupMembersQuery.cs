namespace Comeback.Chat.Application.Features.Conversations.Queries.GetGroupMembers;
using Comeback.Chat.Application.DTOs;
using MediatR;

public sealed record GetGroupMembersQuery(Guid ConversationId, Guid RequesterUserId)
    : IRequest<IReadOnlyList<GroupMemberDto>>;
