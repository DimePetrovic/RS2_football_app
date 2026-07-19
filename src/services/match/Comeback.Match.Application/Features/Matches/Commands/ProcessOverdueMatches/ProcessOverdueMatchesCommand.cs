namespace Comeback.Match.Application.Features.Matches.Commands.ProcessOverdueMatches;

using MediatR;

/// <summary>Daily sweep (Hangfire, 12:00): escalates matches with no entered result.</summary>
public sealed record ProcessOverdueMatchesCommand : IRequest;
