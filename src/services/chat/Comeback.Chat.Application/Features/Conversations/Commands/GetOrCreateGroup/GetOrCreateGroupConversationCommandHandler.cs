namespace Comeback.Chat.Application.Features.Conversations.Commands.GetOrCreateGroup;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Entities;
using Comeback.Chat.Domain.Enums;
using MediatR;

public sealed class GetOrCreateGroupConversationCommandHandler
    : IRequestHandler<GetOrCreateGroupConversationCommand, ConversationSummaryDto>
{
    private readonly IConversationRepository _repository;
    private readonly IChatUnitOfWork _unitOfWork;
    private readonly IChatGroupClient _groupClient;

    public GetOrCreateGroupConversationCommandHandler(
        IConversationRepository repository, IChatUnitOfWork unitOfWork, IChatGroupClient groupClient)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _groupClient = groupClient;
    }

    public async Task<ConversationSummaryDto> Handle(GetOrCreateGroupConversationCommand cmd, CancellationToken ct)
    {
        // Live roster from the Profile service is the single source of truth for membership.
        var group = await _groupClient.GetGroupInfoAsync(cmd.GroupId, ct)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        if (group.Members.All(m => m.UserId != cmd.RequesterUserId))
            throw new ForbiddenException("You are not a member of this group.", "group.not_member");

        var conversation = await _repository.FindGroupConversationAsync(cmd.GroupId, ct);
        if (conversation is null)
        {
            conversation = Conversation.CreateGroup(cmd.GroupId, group.Name, group.AvatarUrl);
            _repository.Add(conversation);
        }
        else if (conversation.Title != group.Name || conversation.GroupAvatarUrl != group.AvatarUrl)
        {
            conversation.UpdateGroupMeta(group.Name, group.AvatarUrl);
        }

        // Materialize a member row for every current group member so the chat appears in their list.
        foreach (var member in group.Members)
            conversation.EnsureMember(member.UserId, member.DisplayName);

        await _unitOfWork.SaveChangesAsync(ct);

        return new ConversationSummaryDto(
            conversation.Id, ConversationType.Group, null, null,
            conversation.GroupId, conversation.Title, conversation.GroupAvatarUrl,
            null, null, HasUnread: false);
    }
}
