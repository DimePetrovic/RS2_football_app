namespace Comeback.Auth.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;

public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    private RefreshToken() { }

    private RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt, DateTime createdAt, string createdByIp)
        : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedByIp = createdByIp;
    }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, string createdByIp) =>
        new(Guid.NewGuid(), userId, token, expiresAt, DateTime.UtcNow, createdByIp);

    public void Revoke(string revokedByIp)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
    }
}
