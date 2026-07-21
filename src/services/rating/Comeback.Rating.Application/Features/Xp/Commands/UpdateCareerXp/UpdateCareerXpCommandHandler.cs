namespace Comeback.Rating.Application.Features.Xp.Commands.UpdateCareerXp;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Domain.Entities;
using MediatR;

internal sealed class UpdateCareerXpCommandHandler : IRequestHandler<UpdateCareerXpCommand>
{
    private readonly IPlayerXpRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCareerXpCommandHandler(IPlayerXpRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCareerXpCommand command, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (existing is null)
        {
            var playerXp = PlayerXp.Create(command.UserId, command.YouthSeasons, command.SeniorSeasons);
            _repository.Add(playerXp);
        }
        else
        {
            existing.UpdateCareerXp(command.YouthSeasons, command.SeniorSeasons);
            _repository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
