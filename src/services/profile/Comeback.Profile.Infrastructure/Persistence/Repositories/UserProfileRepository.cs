namespace Comeback.Profile.Infrastructure.Persistence.Repositories;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Comeback.BuildingBlocks.Domain.Constants;

internal sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly ProfileDbContext _context;

    public UserProfileRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Profiles.FirstOrDefaultAsync(p => p.UserId == id, cancellationToken);

    public Task<List<UserProfile>> GetByIdsAsync(IEnumerable<Guid> profileIds, CancellationToken cancellationToken = default)
    {
        var ids = profileIds.ToList();
        return _context.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> SearchAsync(string query, Guid excludeUserId, int limit, CancellationToken cancellationToken = default)
    {
        // ILIKE koristi PostgreSQL indeks i pravilno hvata velika/mala slova (bez ToLower po redu).
        // Escape-ujemo % i _ iz upita da se tretiraju kao literali, ne kao wildcard-i.
        var pattern = LikePattern.Contains(query);
        return _context.Profiles
            .Where(p => p.UserId != excludeUserId
                     && p.Role != UserRoles.Admin
                     && (EF.Functions.ILike(p.Username, pattern, LikePattern.EscapeChar)
                      || EF.Functions.ILike(p.FirstName, pattern, LikePattern.EscapeChar)
                      || EF.Functions.ILike(p.LastName, pattern, LikePattern.EscapeChar)
                      || EF.Functions.ILike(p.FirstName + " " + p.LastName, pattern, LikePattern.EscapeChar)))
            .OrderBy(p => p.Username)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        return _context.Profiles.Where(p => ids.Contains(p.UserId)).ToListAsync(cancellationToken);
    }

    public Task<List<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.Profiles.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(cancellationToken);

    public void Add(UserProfile profile) => _context.Profiles.Add(profile);

    public void Update(UserProfile profile) => _context.Profiles.Update(profile);
}
