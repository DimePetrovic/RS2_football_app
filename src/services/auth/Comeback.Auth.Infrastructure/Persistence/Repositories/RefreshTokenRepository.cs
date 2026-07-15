namespace Comeback.Auth.Infrastructure.Persistence.Repositories;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context) => _context = context;

    public async Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == token && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task RevokeAllActiveByUserIdAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke(ipAddress);
    }

    public void Add(RefreshToken refreshToken) => _context.RefreshTokens.Add(refreshToken);

    public void Update(RefreshToken refreshToken) => _context.RefreshTokens.Update(refreshToken);
}
