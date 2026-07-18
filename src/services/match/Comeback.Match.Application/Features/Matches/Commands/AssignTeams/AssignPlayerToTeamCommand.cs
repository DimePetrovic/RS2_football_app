namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using Comeback.Match.Domain.Enums;
using MediatR;

public sealed record AssignPlayerToTeamCommand(
    Guid MatchId,
    Guid OrganizerUserId,
    Guid TargetUserId,
    MatchTeam Team) : IRequest;
