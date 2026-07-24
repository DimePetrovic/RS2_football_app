namespace Comeback.Chat.Application.Features.Conversations.Queries.GetMessages;
using Comeback.BuildingBlocks.Application.Clients;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Enums;
using MediatR;

public sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, List<MessageDto>>
{
    private readonly IConversationRepository _repository;
    private readonly IMessageEncryptionService _encryption;
    private readonly IPlayerInfoClient _playerInfo;

    public GetMessagesQueryHandler(
        IConversationRepository repository, IMessageEncryptionService encryption, IPlayerInfoClient playerInfo)
    {
        _repository = repository;
        _encryption = encryption;
        _playerInfo = playerInfo;
    }

    public async Task<List<MessageDto>> Handle(GetMessagesQuery query, CancellationToken ct)
    {
        var conversation = await _repository.GetByIdWithMembersAsync(query.ConversationId, ct)
            ?? throw new NotFoundException("Conversation not found.", "conversation.not_found");

        if (!conversation.HasMember(query.UserId))
            throw new ForbiddenException("You do not have access to this conversation.", "conversation.access_forbidden");

        var messages = await _repository.GetMessagesAsync(query.ConversationId, query.UserId, query.Before, query.Limit, ct);

        // Direct chats mark my messages read once the other member has read past them; groups skip read receipts (v1).
        var otherReadAt = conversation.Type == ConversationType.Direct
            ? conversation.Members.FirstOrDefault(m => m.UserId != query.UserId)?.LastReadAt
            : null;

        // Group messages carry the sender's badge (username + avatar), enriched from the Profile service.
        IReadOnlyDictionary<Guid, PlayerInfo> senderInfos = conversation.Type == ConversationType.Group
            ? (await _playerInfo.GetPlayerInfosAsync(messages.Select(m => m.SenderUserId).Distinct(), ct))
                .ToDictionary(i => i.UserId)
            : new Dictionary<Guid, PlayerInfo>();

        return messages.Select(m =>
        {
            senderInfos.TryGetValue(m.SenderUserId, out var info);
            var senderDisplayName = string.IsNullOrWhiteSpace(info?.DisplayName) ? m.SenderDisplayName : info!.DisplayName!;
            var content = _encryption.TryDecrypt(m.EncryptedContent, out var text) ? text : "[poruka nedostupna]";
            return new MessageDto(
                m.Id, m.ConversationId, m.SenderUserId, senderDisplayName,
                info?.Username, info?.AvatarUrl, info?.Nationality,
                content, m.SentAt,
                m.SenderUserId == query.UserId && otherReadAt.HasValue && m.SentAt <= otherReadAt.Value);
        }).ToList();
    }
}
