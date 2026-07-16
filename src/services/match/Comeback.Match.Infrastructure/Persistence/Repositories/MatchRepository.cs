namespace Comeback.Match.Infrastructure.Persistence.Repositories;

using Comeback.Match.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

public sealed class MatchRepository : IMatchRepository
{
    private readonly MatchDbContext _context;

    public MatchRepository(MatchDbContext context)
        => _context = context;

    public Task<Domain.Entities.Match?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Matches.FindAsync([id], ct).AsTask()!;

    public Task<Domain.Entities.Match?> GetByIdWithParticipantsAsync(Guid id, CancellationToken ct = default)
        => _context.Matches
            .Include(m => m.Participants)
            .Include(m => m.Goals)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<List<Domain.Entities.Match>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Matches
            .Include(m => m.Participants)
            .Include(m => m.Goals)
            .Where(m => m.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(m => m.StartsAt)
            .ToListAsync(ct);

    public async Task<List<Domain.Entities.Match>> GetByGroupIdAsync(Guid groupId, CancellationToken ct = default)
        => await _context.Matches
            .Include(m => m.Participants)
            .Where(m => m.GroupId == groupId || m.OpponentGroupId == groupId)
            .OrderByDescending(m => m.StartsAt)
            .ToListAsync(ct);

    public async Task<List<Domain.Entities.Match>> GetForOverdueSweepAsync(
        DateTime startsAfter, CancellationToken ct = default)
        => await _context.Matches
            .Include(m => m.Participants)
            .Where(m => m.StartsAt >= startsAfter
                     && (m.Status == Domain.Enums.MatchStatus.Scheduled
                      || m.Status == Domain.Enums.MatchStatus.ResultOverdue))
            .ToListAsync(ct);

    public void Add(Domain.Entities.Match match)
        => _context.Matches.Add(match);
}
