namespace Comeback.Chat.Infrastructure.Persistence.Repositories;

using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Entities;
using Comeback.Chat.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _db;

    public ConversationRepository(ChatDbContext db) => _db = db;

    public async Task<Conversation?> FindDirectAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        return await _db.Conversations
            .Include(c => c.Members)
            .Where(c => c.Type == ConversationType.Direct &&
                c.Members.Any(m => m.UserId == userId1) &&
                c.Members.Any(m => m.UserId == userId2) &&
                c.Members.Count == 2)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Conversation?> FindGroupConversationAsync(Guid groupId, CancellationToken ct = default)
    {
        return await _db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.GroupId == groupId, ct);
    }

    public async Task<Conversation?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<ConversationSummaryDto>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default)
    {
        var conversations = await _db.Conversations
            .Include(c => c.Members)
            .Where(c => c.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);

        var hiddenIds = (await _db.HiddenMessages
            .Where(h => h.UserId == userId)
            .Select(h => h.MessageId)
            .ToListAsync(ct)).ToHashSet();

        var summaries = new List<(ConversationSummaryDto Dto, DateTime SortAt)>();

        foreach (var c in conversations)
        {
            var me = c.Members.First(m => m.UserId == userId);

            var last = await _db.Messages
                .Where(m => m.ConversationId == c.Id
                    && (me.ClearedAt == null || m.SentAt > me.ClearedAt)
                    && !hiddenIds.Contains(m.Id))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync(ct);

            // A cleared conversation with nothing new stays hidden until a new message arrives.
            if (me.ClearedAt != null && last == null)
                continue;

            var hasUnread = last != null
                && last.SenderUserId != userId
                && (me.LastReadAt == null || last.SentAt > me.LastReadAt);

            ConversationSummaryDto dto;
            if (c.Type == ConversationType.Group)
            {
                dto = new ConversationSummaryDto(
                    c.Id, ConversationType.Group, null, null, c.GroupId, c.Title, c.GroupAvatarUrl,
                    last?.EncryptedContent, last?.SentAt, hasUnread);
            }
            else
            {
                var other = c.Members.First(m => m.UserId != userId);
                dto = new ConversationSummaryDto(
                    c.Id, ConversationType.Direct, other.UserId, other.DisplayName, null, null, null,
                    last?.EncryptedContent, last?.SentAt, hasUnread);
            }

            summaries.Add((dto, last?.SentAt ?? c.CreatedAt));
        }

        return summaries
            .OrderByDescending(x => x.SortAt)
            .Select(x => x.Dto)
            .ToList();
    }

    public async Task<List<Message>> GetMessagesAsync(
        Guid conversationId, Guid userId, DateTime? before, int limit, CancellationToken ct = default)
    {
        var member = await _db.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId, ct);
        var clearedAt = member?.ClearedAt;

        var hiddenIds = _db.HiddenMessages
            .Where(h => h.UserId == userId)
            .Select(h => h.MessageId);

        var query = _db.Messages.Where(m => m.ConversationId == conversationId && !hiddenIds.Contains(m.Id));
        if (clearedAt.HasValue) query = query.Where(m => m.SentAt > clearedAt.Value);
        if (before.HasValue) query = query.Where(m => m.SentAt < before.Value);

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .ToListAsync(ct);
    }

    public Task<bool> MessageExistsInConversationAsync(
        Guid messageId, Guid conversationId, CancellationToken ct = default)
        => _db.Messages.AnyAsync(m => m.Id == messageId && m.ConversationId == conversationId, ct);

    public Task<bool> IsMessageHiddenAsync(Guid userId, Guid messageId, CancellationToken ct = default)
        => _db.HiddenMessages.AnyAsync(h => h.UserId == userId && h.MessageId == messageId, ct);

    public void Add(Conversation conversation) => _db.Conversations.Add(conversation);
    public void AddMessage(Message message) => _db.Messages.Add(message);
    public void AddHiddenMessage(HiddenMessage hidden) => _db.HiddenMessages.Add(hidden);
}
