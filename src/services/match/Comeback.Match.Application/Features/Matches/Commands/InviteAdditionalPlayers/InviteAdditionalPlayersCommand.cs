namespace Comeback.Match.Application.Features.Matches.Commands.InviteAdditionalPlayers;

using Comeback.Match.Application.Features.Matches.Commands.CreateMatch;
using MediatR;

public sealed record InviteAdditionalPlayersCommand(
    Guid MatchId,
    Guid OrganizerUserId,
    string OrganizerDisplayName,
    IReadOnlyList<InviteeDto> Invitees,
    IReadOnlyList<string>? GuestNames = null) : IRequest;
