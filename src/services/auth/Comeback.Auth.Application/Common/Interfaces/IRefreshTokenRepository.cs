namespace Comeback.Auth.Application.Common.Interfaces;

using Comeback.Auth.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllActiveByUserIdAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default);
    void Add(RefreshToken refreshToken);
    void Update(RefreshToken refreshToken);
}
