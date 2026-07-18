namespace Comeback.Match.Application.Common.Interfaces;

public interface IPlayerRatingService
{
    Task<IReadOnlyList<(Guid UserId, int Rating)>> GetRatingsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);
}
