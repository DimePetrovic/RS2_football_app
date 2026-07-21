namespace Comeback.Match.Application.Features.Matches.Commands.JoinViaPublicCall;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class JoinViaPublicCallCommandHandler : IRequestHandler<JoinViaPublicCallCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;

    public JoinViaPublicCallCommandHandler(
        IMatchRepository matches, IMatchUnitOfWork unitOfWork, IMatchEventPublisher publisher)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task Handle(JoinViaPublicCallCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        match.JoinViaPublicCall(cmd.UserId, cmd.DisplayName);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new PlayerJoinedViaPublicCallIntegrationEvent(
            match.Id, match.Title, match.OrganizerUserId, cmd.UserId, cmd.DisplayName), ct);
    }
}
