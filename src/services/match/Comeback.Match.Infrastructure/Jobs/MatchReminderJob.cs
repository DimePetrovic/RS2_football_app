namespace Comeback.Match.Infrastructure.Jobs;

using Comeback.Match.Application.Features.Matches.Commands.ProcessOverdueMatches;
using Comeback.Match.Application.Features.Matches.Commands.SendResultReminder;
using Hangfire;
using MediatR;

/// <summary>
/// Entry point Hangfire calls; delegates to MediatR commands (the logic lives in the Application layer).
/// </summary>
public sealed class MatchReminderJob
{
    private readonly ISender _sender;

    public MatchReminderJob(ISender sender) => _sender = sender;

    [AutomaticRetry(Attempts = 3)]
    public Task SendResultReminder(Guid matchId)
        => _sender.Send(new SendResultReminderCommand(matchId));

    [AutomaticRetry(Attempts = 2)]
    public Task ProcessOverdueMatches()
        => _sender.Send(new ProcessOverdueMatchesCommand());
}
