namespace Comeback.Social.Application.Common.Interfaces;

public sealed record ProfileBasicInfo(string Username, string? AvatarUrl, string? DisplayName, string? Nationality);

public interface IProfileAvatarsClient
{
    /// <summary>Returns a map userId -> (username, avatarUrl) for the given users.</summary>
    Task<Dictionary<Guid, ProfileBasicInfo>> GetPlayerInfosAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);
}
