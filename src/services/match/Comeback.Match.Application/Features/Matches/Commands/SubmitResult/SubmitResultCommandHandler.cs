namespace Comeback.Match.Application.Features.Matches.Commands.SubmitResult;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class SubmitResultCommandHandler : IRequestHandler<SubmitResultCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public SubmitResultCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(SubmitResultCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        var goals = cmd.Goals
            .Select(g => new GoalEntry(g.ScorerUserId, g.IsOwnGoal, g.AssistUserId))
            .ToList();

        match.SubmitResult(cmd.UserId, cmd.HomeScore, cmd.AwayScore, goals);
        await _unitOfWork.SaveChangesAsync(ct);

        var acceptedParticipants = match.Participants
            .Where(p => p.Status == MatchParticipantStatus.Accepted)
            .ToList();

        // Guests have no account — they receive neither notifications nor XP.
        var notifyUserIds = acceptedParticipants
            .Where(p => !p.IsGuest)
            .Select(p => p.UserId)
            .ToList();

        var players = acceptedParticipants
            .Where(p => p.Team != MatchTeam.None && !p.IsGuest)
            .Select(p => new PlayerMatchResultDto(p.UserId, p.Team.ToString(), p.IsCaptain))
            .ToList();

        var participantInfos = acceptedParticipants
            .Select(p => new ParticipantInfoDto(p.UserId, p.DisplayName))
            .ToList();

        await _publisher.PublishAsync(new MatchResultSubmittedIntegrationEvent(
            match.Id,
            match.Title,
            cmd.HomeScore,
            cmd.AwayScore,
            notifyUserIds,
            players,
            participantInfos), ct);
    }
}
