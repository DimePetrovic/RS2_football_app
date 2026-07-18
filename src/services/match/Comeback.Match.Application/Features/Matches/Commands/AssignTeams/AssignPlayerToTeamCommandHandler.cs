namespace Comeback.Match.Application.Features.Matches.Commands.AssignTeams;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class AssignPlayerToTeamCommandHandler : IRequestHandler<AssignPlayerToTeamCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;

    public AssignPlayerToTeamCommandHandler(IMatchRepository matches, IMatchUnitOfWork unitOfWork)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignPlayerToTeamCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        match.AssignPlayerToTeam(cmd.OrganizerUserId, cmd.TargetUserId, cmd.Team);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
