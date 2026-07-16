namespace Comeback.Match.Application.Features.Matches.Commands.WithdrawFromMatch;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class WithdrawFromMatchCommandHandler : IRequestHandler<WithdrawFromMatchCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public WithdrawFromMatchCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(WithdrawFromMatchCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        match.Withdraw(cmd.UserId);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new MatchParticipantWithdrawnIntegrationEvent(
            match.Id,
            match.Title,
            cmd.UserId,
            cmd.UserDisplayName,
            match.OrganizerUserId), ct);
    }
}
