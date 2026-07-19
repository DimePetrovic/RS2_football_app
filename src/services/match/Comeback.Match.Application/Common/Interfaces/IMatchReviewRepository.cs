namespace Comeback.Match.Application.Common.Interfaces;

using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;

public interface IMatchReviewRepository
{
    Task<MatchPlayerReview?> GetAsync(
        Guid matchId, Guid reviewerParticipantId, Guid reviewedParticipantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<MatchPlayerReview>> GetByMatchAsync(Guid matchId, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerReceivedReviewItem>> GetReceivedByUserAsync(Guid userId, CancellationToken ct = default);

    void Add(MatchPlayerReview review);
}
