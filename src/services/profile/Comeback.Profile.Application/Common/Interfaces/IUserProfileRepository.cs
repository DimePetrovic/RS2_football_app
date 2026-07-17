namespace Comeback.Profile.Application.Common.Interfaces;

using Comeback.Profile.Domain.Entities;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UserProfile>> GetByIdsAsync(IEnumerable<Guid> profileIds, CancellationToken cancellationToken = default);
    Task<List<UserProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<List<UserProfile>> SearchAsync(string query, Guid excludeUserId, int limit, CancellationToken cancellationToken = default);
    Task<List<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(UserProfile profile);
    void Update(UserProfile profile);
}
