namespace Comeback.Chat.Application.Features.Conversations.Commands.DeleteConversationForMe;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using MediatR;

public sealed class DeleteConversationForMeCommandHandler : IRequestHandler<DeleteConversationForMeCommand>
{
    private readonly IConversationRepository _repository;
    private readonly IChatUnitOfWork _unitOfWork;

    public DeleteConversationForMeCommandHandler(IConversationRepository repository, IChatUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteConversationForMeCommand cmd, CancellationToken ct)
    {
        var conversation = await _repository.GetByIdWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new NotFoundException("Conversation not found.", "conversation.not_found");

        // "Delete for me": hide the history for this member only; the conversation and messages are retained.
        var member = conversation.Members.FirstOrDefault(m => m.UserId == cmd.UserId)
            ?? throw new ForbiddenException("You do not have access to this conversation.", "conversation.access_forbidden");

        member.ClearHistory();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
