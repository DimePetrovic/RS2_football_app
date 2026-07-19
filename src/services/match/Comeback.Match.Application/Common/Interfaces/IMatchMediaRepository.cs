namespace Comeback.Match.Application.Common.Interfaces;

using Comeback.Match.Domain.Entities;

public interface IMatchMediaRepository
{
    Task<MatchMedia?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MatchMedia>> GetActiveByMatchAsync(Guid matchId, CancellationToken ct = default);
    void Add(MatchMedia media);
}
