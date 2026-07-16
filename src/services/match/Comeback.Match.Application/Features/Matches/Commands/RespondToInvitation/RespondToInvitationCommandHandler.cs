namespace Comeback.Match.Application.Features.Matches.Commands.RespondToInvitation;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class RespondToInvitationCommandHandler : IRequestHandler<RespondToInvitationCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public RespondToInvitationCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(RespondToInvitationCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        match.RespondToInvitation(cmd.UserId, cmd.Accept);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new MatchInvitationRespondedIntegrationEvent(
            match.Id,
            match.Title,
            cmd.UserId,
            cmd.UserDisplayName,
            match.OrganizerUserId,
            cmd.Accept), ct);
    }
}
