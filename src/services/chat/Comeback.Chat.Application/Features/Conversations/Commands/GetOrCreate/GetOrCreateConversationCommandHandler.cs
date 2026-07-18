namespace Comeback.Chat.Application.Features.Conversations.Commands.GetOrCreate;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.DTOs;
using Comeback.Chat.Domain.Enums;
using Comeback.Chat.Domain.Entities;
using MediatR;

public sealed class GetOrCreateConversationCommandHandler : IRequestHandler<GetOrCreateConversationCommand, ConversationSummaryDto>
{
    private readonly IConversationRepository _repository;
    private readonly IChatUnitOfWork _unitOfWork;

    public GetOrCreateConversationCommandHandler(IConversationRepository repository, IChatUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto> Handle(GetOrCreateConversationCommand cmd, CancellationToken ct)
    {
        if (cmd.RequestingUserId == cmd.OtherUserId)
            throw new BusinessRuleException("You cannot start a conversation with yourself.", "conversation.self");

        var existing = await _repository.FindDirectAsync(cmd.RequestingUserId, cmd.OtherUserId, ct);
        if (existing is not null)
            return DirectSummary(existing.Id, cmd);

        var conversation = Conversation.CreateDirect(
            cmd.RequestingUserId, cmd.RequestingUserDisplayName,
            cmd.OtherUserId, cmd.OtherUserDisplayName);

        _repository.Add(conversation);
        await _unitOfWork.SaveChangesAsync(ct);

        return DirectSummary(conversation.Id, cmd);
    }

    private static ConversationSummaryDto DirectSummary(Guid conversationId, GetOrCreateConversationCommand cmd)
        => new(conversationId, ConversationType.Direct, cmd.OtherUserId, cmd.OtherUserDisplayName,
            null, null, null, null, null, HasUnread: false);
}
