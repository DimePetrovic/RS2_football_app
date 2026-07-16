namespace Comeback.Match.Application.Features.Matches.Commands.RespondToInvitation;

using MediatR;

public sealed record RespondToInvitationCommand(
    Guid MatchId,
    Guid UserId,
    string UserDisplayName,
    bool Accept) : IRequest;
