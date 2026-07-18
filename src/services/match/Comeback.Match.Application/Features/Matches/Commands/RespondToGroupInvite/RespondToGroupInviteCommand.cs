namespace Comeback.Match.Application.Features.Matches.Commands.RespondToGroupInvite;

using MediatR;

public sealed record RespondToGroupInviteCommand(Guid MatchId, Guid CaptainUserId, bool Accept) : IRequest;
