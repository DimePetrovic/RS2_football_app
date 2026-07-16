namespace Comeback.Match.Application.Features.Matches.Commands.CancelMatch;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class CancelMatchCommandHandler : IRequestHandler<CancelMatchCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;
    private readonly IMatchJobScheduler _scheduler;

    public CancelMatchCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher,
        IMatchJobScheduler scheduler)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _scheduler = scheduler;
    }

    public async Task Handle(CancelMatchCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        // Cancel the scheduled reminder before Cancel() clears its id.
        _scheduler.CancelJob(match.ResultReminderJobId);
        match.Cancel(cmd.UserId);
        await _unitOfWork.SaveChangesAsync(ct);

        var notifyUserIds = match.Participants
            .Where(p => p.UserId != cmd.UserId && !p.IsGuest &&
                        (p.Status == MatchParticipantStatus.Accepted ||
                         p.Status == MatchParticipantStatus.Invited))
            .Select(p => p.UserId)
            .ToList();

        if (notifyUserIds.Count > 0)
        {
            await _publisher.PublishAsync(new MatchCancelledIntegrationEvent(
                match.Id,
                match.Title,
                notifyUserIds), ct);
        }
    }
}
