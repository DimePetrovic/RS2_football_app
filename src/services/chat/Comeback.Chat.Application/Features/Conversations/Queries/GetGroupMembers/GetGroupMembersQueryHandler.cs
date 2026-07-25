namespace Comeback.Chat.Application.Features.Conversations.Queries.GetGroupMembers;
using Comeback.BuildingBlocks.Application.Clients;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Enums;
using MediatR;

public sealed class GetGroupMembersQueryHandler
    : IRequestHandler<GetGroupMembersQuery, IReadOnlyList<GroupMemberDto>>
{
    private readonly IConversationRepository _repository;
    private readonly IChatGroupClient _groupClient;
    private readonly IPlayerInfoClient _playerInfo;
    private readonly IChatUnitOfWork _unitOfWork;

    public GetGroupMembersQueryHandler(
        IConversationRepository repository, IChatGroupClient groupClient, IPlayerInfoClient playerInfo,
        IChatUnitOfWork unitOfWork)
    {
        _repository = repository;
        _groupClient = groupClient;
        _playerInfo = playerInfo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<GroupMemberDto>> Handle(GetGroupMembersQuery query, CancellationToken ct)
    {
        var conversation = await _repository.GetByIdWithMembersAsync(query.ConversationId, ct)
            ?? throw new NotFoundException("Conversation not found.", "conversation.not_found");

        if (!conversation.HasMember(query.RequesterUserId))
            throw new ForbiddenException("You do not have access to this conversation.", "conversation.access_forbidden");

        if (conversation.Type != ConversationType.Group || conversation.GroupId is null)
            return [];

        // Live roster from the Profile service, enriched with username + avatar for the badge.
        var group = await _groupClient.GetGroupInfoAsync(conversation.GroupId.Value, ct);
        if (group is null) return [];

        // Opening the chat is a natural sync point for the group's name and avatar.
        if (conversation.Title != group.Name || conversation.GroupAvatarUrl != group.AvatarUrl)
        {
            conversation.UpdateGroupMeta(group.Name, group.AvatarUrl);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var infos = (await _playerInfo.GetPlayerInfosAsync(group.Members.Select(m => m.UserId), ct))
            .ToDictionary(i => i.UserId);

        return group.Members
            .Select(m =>
            {
                infos.TryGetValue(m.UserId, out var info);
                var name = string.IsNullOrWhiteSpace(info?.DisplayName) ? m.DisplayName : info!.DisplayName!;
                return new GroupMemberDto(m.UserId, name, info?.Username, info?.AvatarUrl, info?.Nationality);
            })
            .ToList();
    }
}
