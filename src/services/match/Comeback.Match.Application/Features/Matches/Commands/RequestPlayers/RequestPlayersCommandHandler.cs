namespace Comeback.Match.Application.Features.Matches.Commands.RequestPlayers;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class RequestPlayersCommandHandler : IRequestHandler<RequestPlayersCommand>
{
    private static readonly HashSet<string> ValidPositions =
        ["Goalkeeper", "Defender", "Midfielder", "Forward"];

    private readonly IMatchRepository _matches;
    private readonly IMatchEventPublisher _publisher;

    public RequestPlayersCommandHandler(IMatchRepository matches, IMatchEventPublisher publisher)
    {
        _matches = matches;
        _publisher = publisher;
    }

    public async Task Handle(RequestPlayersCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        if (cmd.RequesterUserId != match.OrganizerUserId && cmd.RequesterUserId != match.SecondOrganizerUserId)
            throw new ForbiddenException("Only the organizer can request players.", "match.organizer_only");

        if (match.Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("Players can only be requested for a scheduled match.", "match.request_only_scheduled");

        var position = cmd.Position;
        if (position is not null && !ValidPositions.Contains(position))
            throw new BusinessRuleException("Invalid player position.", "match.invalid_position");

        // Everyone already tied to the match (organizer + active participants) — excluded from the notification.
        var participantIds = new HashSet<Guid> { match.OrganizerUserId };
        if (match.SecondOrganizerUserId is { } second) participantIds.Add(second);
        foreach (var p in match.Participants)
            if (p.Status is MatchParticipantStatus.Invited or MatchParticipantStatus.Accepted)
                participantIds.Add(p.UserId);

        await _publisher.PublishAsync(new PlayerWantedIntegrationEvent(
            match.Id,
            match.Title,
            match.OrganizerUserId,
            cmd.RequesterDisplayName,
            position,
            participantIds.ToList(),
            match.StartsAt,
            match.Location), ct);
    }
}
