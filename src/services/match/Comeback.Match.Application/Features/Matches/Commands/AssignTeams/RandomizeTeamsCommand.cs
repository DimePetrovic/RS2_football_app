namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using MediatR;

public sealed record RandomizeTeamsCommand(Guid MatchId, Guid OrganizerUserId) : IRequest;
