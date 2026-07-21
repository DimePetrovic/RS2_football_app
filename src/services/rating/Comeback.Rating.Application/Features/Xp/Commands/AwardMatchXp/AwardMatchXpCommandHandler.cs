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

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
