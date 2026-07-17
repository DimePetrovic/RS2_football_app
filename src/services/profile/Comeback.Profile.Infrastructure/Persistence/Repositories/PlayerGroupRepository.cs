namespace Comeback.Profile.Infrastructure.Persistence.Repositories;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class PlayerGroupRepository : IPlayerGroupRepository
{
    private readonly ProfileDbContext _context;

    public PlayerGroupRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public Task<PlayerGroup?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<List<PlayerGroup>> GetByMemberProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
        => _context.Groups
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.ProfileId == profileId))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    public Task<List<PlayerGroup>> SearchByNameAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query}%";
        return _context.Groups
            .Include(g => g.Members)
            .Where(g => EF.Functions.ILike(g.Name, pattern))
            .OrderBy(g => g.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public void Add(PlayerGroup group) => _context.Groups.Add(group);

    public void Remove(PlayerGroup group) => _context.Groups.Remove(group);

    public void TrackMember(PlayerGroupMember member) => _context.GroupMembers.Add(member);
}
