namespace Comeback.Match.Application.Features.Matches.Commands.RespondToGroupInvite;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class RespondToGroupInviteCommandHandler : IRequestHandler<RespondToGroupInviteCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;
    private readonly IPlayerGroupClient _groupClient;

    public RespondToGroupInviteCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher,
        IPlayerGroupClient groupClient)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _groupClient = groupClient;
    }

    public async Task Handle(RespondToGroupInviteCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        IEnumerable<(Guid UserId, string DisplayName)> opponentMembers = [];
        if (cmd.Accept)
        {
            var opponentInfo = await _groupClient.GetGroupMatchInfoAsync(match.OpponentGroupId!.Value, ct)
                ?? throw new NotFoundException("Opponent group not found.", "group.opponent_not_found");
            opponentMembers = opponentInfo.Members.Select(m => (m.UserId, m.DisplayName));
        }

        match.RespondToGroupInvite(cmd.CaptainUserId, cmd.Accept, opponentMembers);
        await _unitOfWork.SaveChangesAsync(ct);

        if (cmd.Accept)
        {
            var organizerDisplayName = match.Participants.First(p => p.IsOrganizer).DisplayName;
            foreach (var member in match.Participants.Where(p => p.GroupSide == MatchTeam.Away))
            {
                await _publisher.PublishAsync(new MatchInvitationSentIntegrationEvent(
                    match.Id, match.Title, match.OrganizerUserId, organizerDisplayName,
                    member.UserId, match.StartsAt, match.Location), ct);
            }
        }

        await _publisher.PublishAsync(new GroupMatchInviteRespondedIntegrationEvent(
            match.Id, match.Title, match.OrganizerUserId, match.OpponentGroupName!, cmd.Accept), ct);
    }
}
