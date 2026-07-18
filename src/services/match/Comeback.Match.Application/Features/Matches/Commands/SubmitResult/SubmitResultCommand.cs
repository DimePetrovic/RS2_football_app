namespace Comeback.Match.Application.Features.Matches.Commands.SubmitResult;

using Comeback.Match.Application.DTOs;
using MediatR;

public sealed record SubmitResultCommand(
    Guid MatchId,
    Guid UserId,
    int HomeScore,
    int AwayScore,
    IReadOnlyList<GoalEntryDto> Goals) : IRequest;
