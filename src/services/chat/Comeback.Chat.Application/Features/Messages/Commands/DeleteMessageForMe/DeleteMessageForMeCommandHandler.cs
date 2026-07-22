namespace Comeback.Chat.Application.Features.Messages.Commands.DeleteMessageForMe;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Domain.Entities;
using MediatR;

public sealed class DeleteMessageForMeCommandHandler : IRequestHandler<DeleteMessageForMeCommand>
{
    private readonly IConversationRepository _repository;
    private readonly IChatUnitOfWork _unitOfWork;

    public DeleteMessageForMeCommandHandler(IConversationRepository repository, IChatUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteMessageForMeCommand cmd, CancellationToken ct)
    {
        var conversation = await _repository.GetByIdWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new NotFoundException("Conversation not found.", "conversation.not_found");

        if (!conversation.HasMember(cmd.UserId))
            throw new ForbiddenException("You do not have access to this conversation.", "conversation.access_forbidden");

        if (!await _repository.MessageExistsInConversationAsync(cmd.MessageId, cmd.ConversationId, ct))
            throw new NotFoundException("Message not found.", "message.not_found");

        // "Delete for me": idempotent — the message is retained; only hidden for this user.
        if (await _repository.IsMessageHiddenAsync(cmd.UserId, cmd.MessageId, ct))
            return;

        _repository.AddHiddenMessage(new HiddenMessage(cmd.UserId, cmd.MessageId));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
