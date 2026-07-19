namespace Comeback.Match.Infrastructure.Persistence.Repositories;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class MatchReviewRepository : IMatchReviewRepository
{
    private readonly MatchDbContext _context;

    public MatchReviewRepository(MatchDbContext context)
        => _context = context;

    public Task<MatchPlayerReview?> GetAsync(
        Guid matchId, Guid reviewerParticipantId, Guid reviewedParticipantId,
        CancellationToken ct = default)
        => _context.Reviews
            .FirstOrDefaultAsync(r =>
                r.MatchId == matchId &&
                r.ReviewerParticipantId == reviewerParticipantId &&
                r.ReviewedParticipantId == reviewedParticipantId, ct);

    public async Task<IReadOnlyList<MatchPlayerReview>> GetByMatchAsync(Guid matchId, CancellationToken ct = default)
        => await _context.Reviews
            .Where(r => r.MatchId == matchId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PlayerReceivedReviewItem>> GetReceivedByUserAsync(
        Guid userId, CancellationToken ct = default)
        => await (
            from r in _context.Reviews
            join reviewedP in _context.Participants on r.ReviewedParticipantId equals reviewedP.Id
            join reviewerP in _context.Participants on r.ReviewerParticipantId equals reviewerP.Id
            join m in _context.Matches on r.MatchId equals m.Id
            where reviewedP.UserId == userId
            orderby r.CreatedAt descending
            select new PlayerReceivedReviewItem(
                m.Id, m.Title,
                reviewerP.UserId,
                reviewerP.DisplayName,
                // Username/avatar/nationality are enriched from the Profile service in the query handler.
                null, null, null,
                r.OverallRating, r.GoalkeepingRating, r.DefenseRating, r.AttackRating, r.EffortRating,
                r.Comment, r.CreatedAt)
        ).ToListAsync(ct);

    public void Add(MatchPlayerReview review)
        => _context.Reviews.Add(review);
}
