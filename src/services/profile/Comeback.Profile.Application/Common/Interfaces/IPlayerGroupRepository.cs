namespace Comeback.Profile.Application.Common.Interfaces;

using Comeback.Profile.Domain.Entities;

public interface IPlayerGroupRepository
{
    Task<PlayerGroup?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PlayerGroup>> GetByMemberProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<List<PlayerGroup>> SearchByNameAsync(string query, int limit, CancellationToken cancellationToken = default);
    void Add(PlayerGroup group);
    void Remove(PlayerGroup group);
    void TrackMember(PlayerGroupMember member);
}
