namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class RandomizeTeamsCommandHandler : IRequestHandler<RandomizeTeamsCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;

    public RandomizeTeamsCommandHandler(IMatchRepository matches, IMatchUnitOfWork unitOfWork)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RandomizeTeamsCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        match.RandomizeTeams(cmd.OrganizerUserId);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
