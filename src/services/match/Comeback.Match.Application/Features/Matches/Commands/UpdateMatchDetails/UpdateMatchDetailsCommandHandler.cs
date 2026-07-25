namespace Comeback.Match.Application.Features.Matches.Commands.UpdateMatchDetails;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class UpdateMatchDetailsCommandHandler : IRequestHandler<UpdateMatchDetailsCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;
    private readonly IMatchJobScheduler _scheduler;

    public UpdateMatchDetailsCommandHandler(
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

    public async Task Handle(UpdateMatchDetailsCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        // Validate + apply the edit first; UpdateDetails enforces organizer/status rules and may throw,
        // in which case we must not have already cancelled the existing reminder job.
        var previousEndsAt = match.EndsAt;
        match.UpdateDetails(cmd.OrganizerUserId, cmd.Title, cmd.Location, cmd.StartsAt, cmd.DurationMinutes);

        // Only touch the scheduler when the match end time actually changed (a title/location edit leaves
        // the reminder untouched). And never schedule a reminder in the past — editing an already-ended
        // match must not fire an instant "enter the result" reminder.
        if (match.EndsAt != previousEndsAt)
        {
            _scheduler.CancelJob(match.ResultReminderJobId);
            var reminderAt = match.EndsAt.AddMinutes(15);
            var jobId = reminderAt > DateTime.UtcNow
                ? _scheduler.ScheduleResultReminder(match.Id, reminderAt)
                : null;
            match.SetResultReminderJobId(jobId);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var notifyUserIds = match.Participants
            .Where(p => !p.IsOrganizer && !p.IsGuest &&
                (p.Status == MatchParticipantStatus.Accepted || p.Status == MatchParticipantStatus.Invited))
            .Select(p => p.UserId)
            .ToList();

        if (notifyUserIds.Count > 0)
        {
            await _publisher.PublishAsync(new MatchDetailsUpdatedIntegrationEvent(
                match.Id, match.Title, notifyUserIds), ct);
        }
    }
}
