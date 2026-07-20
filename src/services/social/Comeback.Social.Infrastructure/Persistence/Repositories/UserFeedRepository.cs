namespace Comeback.Social.Infrastructure.Persistence.Repositories;

using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class UserFeedRepository : IUserFeedRepository
{
    private readonly SocialDbContext _context;

    public UserFeedRepository(SocialDbContext context) => _context = context;

    public Task<List<UserFeedItem>> GetFeedAsync(Guid userId, int skip, int take, CancellationToken ct = default)
        => _context.UserFeedItems
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<List<Guid>> GetExistingUserIdsWithPostAsync(
        Guid postId, IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.ToList();
        return _context.UserFeedItems
            .Where(f => f.PostId == postId && ids.Contains(f.UserId))
            .Select(f => f.UserId)
            .ToListAsync(ct);
    }

    public Task<List<Guid>> GetUserIdsForPostAsync(Guid postId, CancellationToken ct = default)
        => _context.UserFeedItems
            .Where(f => f.PostId == postId)
            .Select(f => f.UserId)
            .ToListAsync(ct);

    public void AddRange(IEnumerable<UserFeedItem> items) => _context.UserFeedItems.AddRange(items);
}
