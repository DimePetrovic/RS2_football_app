namespace Comeback.Match.Application.Features.Matches.Commands.JoinViaPublicCall;
using MediatR;

public sealed record JoinViaPublicCallCommand(Guid MatchId, Guid UserId, string DisplayName) : IRequest;
