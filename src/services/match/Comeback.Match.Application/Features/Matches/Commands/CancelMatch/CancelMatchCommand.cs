namespace Comeback.Match.Application.Features.Matches.Commands.CancelMatch;

using MediatR;

public sealed record CancelMatchCommand(Guid MatchId, Guid UserId) : IRequest;
