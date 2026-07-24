namespace Comeback.Chat.Application.Features.Conversations.Queries.GetConversations;
using Comeback.BuildingBlocks.Application.Clients;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Enums;
using MediatR;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, List<ConversationSummaryDto>>
{
    private readonly IConversationRepository _repository;
    private readonly IMessageEncryptionService _encryption;
    private readonly IPlayerInfoClient _playerInfo;

    public GetConversationsQueryHandler(
        IConversationRepository repository, IMessageEncryptionService encryption, IPlayerInfoClient playerInfo)
    {
        _repository = repository;
        _encryption = encryption;
        _playerInfo = playerInfo;
    }

    public async Task<List<ConversationSummaryDto>> Handle(GetConversationsQuery query, CancellationToken ct)
    {
        var conversations = await _repository.GetUserConversationsAsync(query.UserId, ct);

        // Direct conversations use the other participant's avatar as the icon (group avatars come from the group).
        var directUserIds = conversations
            .Where(c => c.Type == ConversationType.Direct && c.OtherUserId is not null)
            .Select(c => c.OtherUserId!.Value)
            .Distinct()
            .ToList();
        var avatarByUser = directUserIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : (await _playerInfo.GetPlayerInfosAsync(directUserIds, ct))
                .ToDictionary(i => i.UserId, i => i.AvatarUrl);

        return conversations.Select(c => c with
        {
            LastMessagePreview = c.LastMessagePreview != null
                ? (_encryption.TryDecrypt(c.LastMessagePreview, out var preview) ? preview : "[poruka nedostupna]")
                : null,
            AvatarUrl = c.Type == ConversationType.Direct && c.OtherUserId is { } uid
                ? avatarByUser.GetValueOrDefault(uid)
                : c.AvatarUrl,
        }).ToList();
    }
}
