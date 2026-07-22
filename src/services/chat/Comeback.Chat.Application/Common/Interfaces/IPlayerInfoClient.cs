namespace Comeback.Chat.Application.Common.Interfaces;

public sealed record PlayerInfo(Guid UserId, string Username, string? AvatarUrl, string? DisplayName, string? Nationality);

public interface IPlayerInfoClient
{
    /// <summary>Public profile data (username + avatar) used to render the sender badge in group chats.</summary>
    Task<IReadOnlyList<PlayerInfo>> GetPlayerInfosAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
