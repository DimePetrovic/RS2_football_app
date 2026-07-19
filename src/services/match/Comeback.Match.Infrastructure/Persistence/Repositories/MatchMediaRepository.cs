namespace Comeback.Match.Infrastructure.Persistence.Repositories;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class MatchMediaRepository : IMatchMediaRepository
{
    private readonly MatchDbContext _context;

    public MatchMediaRepository(MatchDbContext context)
        => _context = context;

    public Task<MatchMedia?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Media.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<MatchMedia>> GetActiveByMatchAsync(Guid matchId, CancellationToken ct = default)
        => await _context.Media
            .Where(m => m.MatchId == matchId && m.Status == MatchMediaStatus.Active)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

    public void Add(MatchMedia media)
        => _context.Media.Add(media);
}
