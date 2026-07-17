namespace Comeback.Profile.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Profile.Domain.Enums;

public sealed class PlayerGroupMember : Entity<Guid>
{
    public Guid GroupId { get; private set; }
    public Guid ProfileId { get; private set; }
    public GroupMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private PlayerGroupMember() { }

    private PlayerGroupMember(Guid id, Guid groupId, Guid profileId, GroupMemberRole role) : base(id)
    {
        GroupId = groupId;
        ProfileId = profileId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    internal static PlayerGroupMember Create(Guid groupId, Guid profileId, GroupMemberRole role)
        => new(Guid.NewGuid(), groupId, profileId, role);

    internal void PromoteToCaptain() => Role = GroupMemberRole.Captain;
    internal void DemoteToMember() => Role = GroupMemberRole.Member;
}
