namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using MediatR;

public sealed record RandomizeTeamsWithCaptainsCommand(
    Guid MatchId,
    Guid OrganizerUserId,
    Guid HomeCaptainId,
    Guid AwayCaptainId) : IRequest;
