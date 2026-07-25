namespace Comeback.BuildingBlocks.Application.Clients;

/// <summary>Basic public profile data (username + avatar + display name) for rendering a player.</summary>
public sealed record PlayerInfo(Guid UserId, string Username, string? AvatarUrl, string? DisplayName, string? Nationality);

/// <summary>
/// Fetches public profile data from the Profile service's internal avatars endpoint.
/// Shared across services (match, chat, …) so the endpoint contract lives in one place.
/// </summary>
public interface IPlayerInfoClient
{
    Task<IReadOnlyList<PlayerInfo>> GetPlayerInfosAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
