namespace Comeback.Match.Application.Features.Matches.Commands.UpdateMatchDetails;

using MediatR;

public sealed record UpdateMatchDetailsCommand(
    Guid MatchId,
    Guid OrganizerUserId,
    string Title,
    string? Location,
    DateTime StartsAt,
    int? DurationMinutes) : IRequest;
