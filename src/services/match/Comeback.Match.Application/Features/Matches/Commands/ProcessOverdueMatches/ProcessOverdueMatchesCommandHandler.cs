namespace Comeback.Match.Application.Features.Matches.Commands.ProcessOverdueMatches;

using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class ProcessOverdueMatchesCommandHandler : IRequestHandler<ProcessOverdueMatchesCommand>
{
    // Search window: wide enough to also catch matches missed if the sweep was skipped once.
    private static readonly TimeSpan SweepWindow = TimeSpan.FromDays(7);

    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public ProcessOverdueMatchesCommandHandler(
        IMatchRepository matches, IMatchUnitOfWork unitOfWork, IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(ProcessOverdueMatchesCommand cmd, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await _matches.GetForOverdueSweepAsync(now - SweepWindow, ct);

        var events = new List<object>();

        foreach (var match in candidates)
        {
            if (match.Status == MatchStatus.Scheduled && now >= match.EndsAt)
            {
                // A scheduled match past its deadline -> "result entry is overdue".
                match.MarkResultOverdue();
                events.Add(new MatchResultOverdueIntegrationEvent(
                    match.Id, match.Title, match.OrganizerUserId));
            }
            else if (match.Status == MatchStatus.ResultOverdue)
            {
                // Still overdue since the previous sweep -> missed.
                match.MarkMissed();
                var notify = match.Participants
                    .Where(p => p.Status == MatchParticipantStatus.Accepted)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToList();
                events.Add(new MatchMissedIntegrationEvent(match.Id, match.Title, notify));
            }
        }

        if (events.Count == 0) return;

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var e in events)
        {
            switch (e)
            {
                case MatchResultOverdueIntegrationEvent overdue:
                    await _publisher.PublishAsync(overdue, ct);
                    break;
                case MatchMissedIntegrationEvent missed:
                    await _publisher.PublishAsync(missed, ct);
                    break;
            }
        }
    }
}
