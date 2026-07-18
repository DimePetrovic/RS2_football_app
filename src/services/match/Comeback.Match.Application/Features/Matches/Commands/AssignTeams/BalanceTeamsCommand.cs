namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using MediatR;

public sealed record BalanceTeamsCommand(Guid MatchId, Guid OrganizerUserId) : IRequest;
