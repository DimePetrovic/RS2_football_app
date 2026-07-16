namespace Comeback.Match.Application.Features.Matches.Commands.WithdrawFromMatch;

using MediatR;

public sealed record WithdrawFromMatchCommand(
    Guid MatchId,
    Guid UserId,
    string UserDisplayName) : IRequest;
