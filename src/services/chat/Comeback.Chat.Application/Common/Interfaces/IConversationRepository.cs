namespace Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Entities;

public interface IConversationRepository
{
    Task<Conversation?> FindDirectAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<Conversation?> FindGroupConversationAsync(Guid groupId, CancellationToken ct = default);
    Task<Conversation?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default);
    Task<List<ConversationSummaryDto>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default);
    Task<List<Message>> GetMessagesAsync(Guid conversationId, Guid userId, DateTime? before, int limit, CancellationToken ct = default);
    Task<bool> MessageExistsInConversationAsync(Guid messageId, Guid conversationId, CancellationToken ct = default);
    Task<bool> IsMessageHiddenAsync(Guid userId, Guid messageId, CancellationToken ct = default);
    void Add(Conversation conversation);
    void AddMessage(Message message);
    void AddHiddenMessage(HiddenMessage hidden);
}
