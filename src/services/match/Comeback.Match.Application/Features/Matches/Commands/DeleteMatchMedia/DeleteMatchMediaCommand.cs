namespace Comeback.Match.Application.Features.Matches.Commands.DeleteMatchMedia;

using MediatR;

public sealed record DeleteMatchMediaCommand(
    Guid MatchId,
    Guid MediaId,
    Guid UserId) : IRequest;
