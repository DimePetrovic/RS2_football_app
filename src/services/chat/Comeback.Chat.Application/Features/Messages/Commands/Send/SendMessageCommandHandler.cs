namespace Comeback.Chat.Application.Features.Messages.Commands.Send;
using Comeback.BuildingBlocks.Application.Clients;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Entities;
using Comeback.Chat.Domain.Enums;
using MediatR;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IConversationRepository _repository;
    private readonly IChatUnitOfWork _unitOfWork;
    private readonly IMessageEncryptionService _encryption;
    private readonly IChatGroupClient _groupClient;
    private readonly IPlayerInfoClient _playerInfo;

    public SendMessageCommandHandler(
        IConversationRepository repository,
        IChatUnitOfWork unitOfWork,
        IMessageEncryptionService encryption,
        IChatGroupClient groupClient,
        IPlayerInfoClient playerInfo)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _encryption = encryption;
        _groupClient = groupClient;
        _playerInfo = playerInfo;
    }

    public async Task<SendMessageResult> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var conversation = await _repository.GetByIdWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new NotFoundException("Conversation not found.", "conversation.not_found");

        if (string.IsNullOrWhiteSpace(cmd.Content))
            throw new BusinessRuleException("Message cannot be empty.", "message.empty");

        List<Guid> recipientIds;
        string senderDisplayName = cmd.SenderDisplayName;
        string? senderUsername = null;
        string? senderAvatarUrl = null;
        string? senderNationality = null;

        if (conversation.Type == ConversationType.Group)
        {
            // Live roster decides who may send and who receives; new members are materialized on the fly.
            var group = await _groupClient.GetGroupInfoAsync(conversation.GroupId!.Value, ct)
                ?? throw new NotFoundException("Group not found.", "group.not_found");

            if (group.Members.All(m => m.UserId != cmd.SenderUserId))
                throw new ForbiddenException("You cannot send messages in this conversation.", "conversation.send_forbidden");

            foreach (var member in group.Members)
                conversation.EnsureMember(member.UserId, member.DisplayName);

            if (conversation.Title != group.Name || conversation.GroupAvatarUrl != group.AvatarUrl)
                conversation.UpdateGroupMeta(group.Name, group.AvatarUrl);

            recipientIds = group.Members.Select(m => m.UserId).ToList();

            var senderInfo = (await _playerInfo.GetPlayerInfosAsync(new[] { cmd.SenderUserId }, ct)).FirstOrDefault();
            senderUsername = senderInfo?.Username;
            senderAvatarUrl = senderInfo?.AvatarUrl;
            senderNationality = senderInfo?.Nationality;
            if (!string.IsNullOrWhiteSpace(senderInfo?.DisplayName))
                senderDisplayName = senderInfo!.DisplayName!;
        }
        else
        {
            if (!conversation.HasMember(cmd.SenderUserId))
                throw new ForbiddenException("You cannot send messages in this conversation.", "conversation.send_forbidden");

            recipientIds = conversation.Members.Select(m => m.UserId).ToList();
        }

        var encrypted = _encryption.Encrypt(cmd.Content);
        // Persist the same resolved name we broadcast in the DTO below (for groups this is the live
        // profile DisplayName), so the stored row and the pushed message never disagree.
        var message = Message.Create(cmd.ConversationId, cmd.SenderUserId, senderDisplayName, encrypted);

        _repository.AddMessage(message);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new MessageDto(message.Id, message.ConversationId, message.SenderUserId,
            senderDisplayName, senderUsername, senderAvatarUrl, senderNationality, cmd.Content, message.SentAt, IsRead: false);

        return new SendMessageResult(dto, recipientIds);
    }
}
