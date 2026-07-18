namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class BalanceTeamsCommandHandler : IRequestHandler<BalanceTeamsCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IPlayerRatingService _ratingService;

    public BalanceTeamsCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IPlayerRatingService ratingService)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _ratingService = ratingService;
    }

    public async Task Handle(BalanceTeamsCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        var acceptedUserIds = match.Participants
            .Where(p => p.Status == Comeback.Match.Domain.Enums.MatchParticipantStatus.Accepted)
            .Select(p => p.UserId);

        var ratings = await _ratingService.GetRatingsAsync(acceptedUserIds, ct);
        match.BalanceTeams(cmd.OrganizerUserId, ratings);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
