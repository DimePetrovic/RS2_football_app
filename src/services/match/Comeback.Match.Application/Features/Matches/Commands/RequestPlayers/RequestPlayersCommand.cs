namespace Comeback.Match.Application.Features.Matches.Commands.RequestPlayers;
using MediatR;

/// <summary>Organizer publishes a public "player wanted" call for their match. Position is null for any position.</summary>
public sealed record RequestPlayersCommand(
    Guid MatchId,
    Guid RequesterUserId,
    string RequesterDisplayName,
    string? Position) : IRequest;
