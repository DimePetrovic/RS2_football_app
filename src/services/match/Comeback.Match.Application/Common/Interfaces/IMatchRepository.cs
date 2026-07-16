namespace Comeback.Match.Application.Common.Interfaces;

using Comeback.Match.Domain.Entities;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Match?> GetByIdWithParticipantsAsync(Guid id, CancellationToken ct = default);
    Task<List<Match>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Match>> GetByGroupIdAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Matches (scheduled or with an overdue result) starting from the given moment — for the daily sweep.</summary>
    Task<List<Match>> GetForOverdueSweepAsync(DateTime startsAfter, CancellationToken ct = default);

    void Add(Match match);
}
