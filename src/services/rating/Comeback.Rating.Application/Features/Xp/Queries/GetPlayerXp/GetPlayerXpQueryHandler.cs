namespace Comeback.Rating.Application.Features.Xp.Queries.GetPlayerXp;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Application.DTOs;
using Comeback.Rating.Domain.Entities;
using MediatR;

internal sealed class GetPlayerXpQueryHandler : IRequestHandler<GetPlayerXpQuery, PlayerXpResponse>
{
    private readonly IPlayerXpRepository _repository;

    public GetPlayerXpQueryHandler(IPlayerXpRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlayerXpResponse> Handle(GetPlayerXpQuery request, CancellationToken cancellationToken)
    {
        var playerXp = await _repository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Player XP record not found.");

        return ToResponse(playerXp);
    }

    private static PlayerXpResponse ToResponse(PlayerXp p)
    {
        var nextLevelXp = (int)(400 * Math.Pow(p.Level, 2));
        var xpToNextLevel = Math.Max(0, nextLevelXp - p.TotalXp);

        return new PlayerXpResponse(
            p.UserId,
            p.TotalXp,
            p.Level,
            p.CareerXp,
            p.MatchXp,
            p.YouthSeasons,
            p.SeniorSeasons,
            xpToNextLevel,
            p.UpdatedAt);
    }
}
