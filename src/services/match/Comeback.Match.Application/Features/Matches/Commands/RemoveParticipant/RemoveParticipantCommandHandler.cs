namespace Comeback.Match.Application.Features.Matches.Commands.RemoveParticipant;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class RemoveParticipantCommandHandler : IRequestHandler<RemoveParticipantCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;

    public RemoveParticipantCommandHandler(IMatchRepository matches, IMatchUnitOfWork unitOfWork)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveParticipantCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        match.RemoveParticipant(cmd.OrganizerUserId, cmd.TargetUserId);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
