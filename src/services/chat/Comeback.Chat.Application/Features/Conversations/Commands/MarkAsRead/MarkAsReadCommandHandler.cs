namespace Comeback.Chat.Application.Features.Conversations.Commands.MarkAsRead;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Chat.Application.Common.Interfaces;
using MediatR;

public sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, DateTime>
{
    private readonly IConversationRepository _repository;
    private readonly IChatUnitOfWork _unitOfWork;

    public MarkAsReadCommandHandler(IConversationRepository repository, IChatUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DateTime> Handle(MarkAsReadCommand cmd, CancellationToken ct)
    {
        var conversation = await _repository.GetByIdWithMembersAsync(cmd.ConversationId, ct)
            ?? throw new NotFoundException("Conversation not found.");

        var member = conversation.Members.FirstOrDefault(m => m.UserId == cmd.UserId)
            ?? throw new ForbiddenException("Not a member of this conversation.");

        member.MarkAsRead();
        await _unitOfWork.SaveChangesAsync(ct);

        return member.LastReadAt!.Value;
    }
}
