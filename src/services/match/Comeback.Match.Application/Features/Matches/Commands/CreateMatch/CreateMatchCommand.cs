namespace Comeback.Match.Application.Features.Matches.Commands.CreateMatch;

using Comeback.Match.Domain.Enums;
using MediatR;

public sealed record InviteeDto(Guid UserId, string DisplayName);

public sealed record CreateMatchCommand(
    string Title,
    MatchType Type,
    Guid OrganizerUserId,
    string OrganizerDisplayName,
    string? Location,
    DateTime StartsAt,
    int? DurationMinutes,
    int PlayersPerTeam,
    int MaxSubstitutes,
    IReadOnlyList<InviteeDto> Invitees,
    Guid? GroupId,
    Guid? OpponentGroupId,
    IReadOnlyList<string>? GuestNames = null) : IRequest<Guid>;
