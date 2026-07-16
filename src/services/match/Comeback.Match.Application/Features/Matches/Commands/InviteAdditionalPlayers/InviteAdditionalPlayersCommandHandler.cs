namespace Comeback.Match.Application.Features.Matches.Commands.InviteAdditionalPlayers;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class InviteAdditionalPlayersCommandHandler : IRequestHandler<InviteAdditionalPlayersCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public InviteAdditionalPlayersCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(InviteAdditionalPlayersCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        foreach (var invitee in cmd.Invitees)
            match.InvitePlayer(cmd.OrganizerUserId, invitee.UserId, invitee.DisplayName);

        foreach (var guestName in cmd.GuestNames ?? [])
            match.AddGuest(cmd.OrganizerUserId, guestName);

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var invitee in cmd.Invitees)
        {
            await _publisher.PublishAsync(new MatchInvitationSentIntegrationEvent(
                match.Id,
                match.Title,
                match.OrganizerUserId,
                cmd.OrganizerDisplayName,
                invitee.UserId,
                match.StartsAt,
                match.Location), ct);
        }
    }
}
