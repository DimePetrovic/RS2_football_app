namespace Comeback.Match.Application.Features.Matches.Commands.SendResultReminder;

using MediatR;

/// <summary>Fired by Hangfire 15min after the match ends — reminds the organizer to enter the result.</summary>
public sealed record SendResultReminderCommand(Guid MatchId) : IRequest;
