namespace Comeback.Match.Application.Features.Matches.Commands.RemoveParticipant;

using MediatR;

public sealed record RemoveParticipantCommand(
    Guid MatchId,
    Guid OrganizerUserId,
    Guid TargetUserId) : IRequest;
