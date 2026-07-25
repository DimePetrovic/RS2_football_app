namespace Comeback.Rating.Application.Features.Xp.Commands.AwardMatchXp;

using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Domain.Entities;
using MediatR;

internal sealed class AwardMatchXpCommandHandler : IRequestHandler<AwardMatchXpCommand>
{
    private readonly IPlayerXpRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AwardMatchXpCommandHandler(IPlayerXpRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AwardMatchXpCommand cmd, CancellationToken ct)
    {
        // Idempotency guard: if this match's XP was already awarded to this player
        // (e.g. the MatchResultSubmitted event was redelivered/retried), do nothing.
        if (await _repository.HasAwardedMatchXpAsync(cmd.MatchId, cmd.UserId, ct))
            return;

        var playerXp = await _repository.GetByUserIdAsync(cmd.UserId, ct);
        if (playerXp is null)
        {
            playerXp = PlayerXp.Create(cmd.UserId, 0, 0);
            playerXp.AddMatchXp(cmd.Amount);
            _repository.Add(playerXp);
        }
        else
        {
            playerXp.AddMatchXp(cmd.Amount);
            _repository.Update(playerXp);
        }

        // Persisted in the same transaction as the XP change; the composite (MatchId, UserId)
        // primary key is the hard guarantee that the award commits at most once.
        _repository.MarkMatchXpAwarded(new AwardedMatchXp(cmd.MatchId, cmd.UserId));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
