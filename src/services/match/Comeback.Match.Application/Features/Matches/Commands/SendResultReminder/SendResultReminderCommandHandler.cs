namespace Comeback.Match.Application.Features.Matches.Commands.SendResultReminder;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class SendResultReminderCommandHandler : IRequestHandler<SendResultReminderCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public SendResultReminderCommandHandler(
        IMatchRepository matches, IMatchUnitOfWork unitOfWork, IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(SendResultReminderCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdAsync(cmd.MatchId, ct);

        // Remind only if the match is still awaiting a result (not entered, cancelled, or missed).
        if (match is null || match.HasResult
            || (match.Status != MatchStatus.Scheduled && match.Status != MatchStatus.ResultOverdue))
            return;

        // The job has done its work — clear the id so cancellation is not attempted later.
        match.SetResultReminderJobId(null);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new MatchResultReminderIntegrationEvent(
            match.Id, match.Title, match.OrganizerUserId), ct);
    }
}
