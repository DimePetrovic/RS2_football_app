namespace Comeback.Social.Infrastructure.Persistence.Repositories;

using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Domain.Entities;
using Comeback.Social.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class PostRepository : IPostRepository
{
    private readonly SocialDbContext _context;

    public PostRepository(SocialDbContext context) => _context = context;

    public Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Posts
            .Include(p => p.Participants)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Post?> GetByMatchIdAsync(Guid matchId, CancellationToken ct = default)
        => _context.Posts.FirstOrDefaultAsync(p => p.MatchId == matchId, ct);

    public Task<Post?> GetByMatchIdAndTypeAsync(Guid matchId, PostType type, CancellationToken ct = default)
        => _context.Posts.FirstOrDefaultAsync(p => p.MatchId == matchId && p.Type == type, ct);

    public Task<List<Post>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return _context.Posts
            .Include(p => p.Participants)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task<List<PostComment>> GetCommentsAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await _context.Posts
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);
        return post?.Comments.ToList() ?? [];
    }

    public void Add(Post post) => _context.Posts.Add(post);
}
