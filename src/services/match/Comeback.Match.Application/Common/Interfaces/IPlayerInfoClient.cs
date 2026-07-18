namespace Comeback.Match.Application.Common.Interfaces;

public sealed record PlayerInfo(Guid UserId, string Username, string? AvatarUrl, string? DisplayName, string? Nationality);

public interface IPlayerInfoClient
{
    /// <summary>Basic public profile data (username + avatar) for displaying a player.</summary>
    Task<IReadOnlyList<PlayerInfo>> GetPlayerInfosAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
