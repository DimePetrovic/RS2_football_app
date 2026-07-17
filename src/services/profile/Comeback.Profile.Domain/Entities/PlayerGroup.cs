namespace Comeback.Profile.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Profile.Domain.Enums;

public sealed class PlayerGroup : AggregateRoot<Guid>
{
    private List<PlayerGroupMember> _members = [];

    public string Name { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<PlayerGroupMember> Members => _members.AsReadOnly();

    private PlayerGroup() { }

    private PlayerGroup(Guid id, string name, string? avatarUrl) : base(id)
    {
        Name = name;
        AvatarUrl = avatarUrl;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static PlayerGroup Create(string name, string? avatarUrl, Guid captainProfileId)
    {
        var group = new PlayerGroup(Guid.NewGuid(), name, avatarUrl);
        group._members.Add(PlayerGroupMember.Create(group.Id, captainProfileId, GroupMemberRole.Captain));
        return group;
    }

    public void AddMember(Guid profileId, Guid requestingProfileId)
    {
        EnsureIsCaptain(requestingProfileId);
        if (_members.Any(m => m.ProfileId == profileId))
            throw new ConflictException("The player is already a member of this group.", "group.member_exists");
        _members.Add(PlayerGroupMember.Create(Id, profileId, GroupMemberRole.Member));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveMember(Guid profileId, Guid requestingProfileId)
    {
        EnsureIsCaptain(requestingProfileId);
        var member = _members.FirstOrDefault(m => m.ProfileId == profileId)
            ?? throw new NotFoundException("The player is not a member of this group.", "group.member_not_found");
        if (member.Role == GroupMemberRole.Captain)
            throw new BusinessRuleException("The captain cannot be removed. Transfer captaincy first.", "group.captain_cannot_leave");
        _members.Remove(member);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Returns true if the group should be deleted (only 1 member left).</summary>
    public bool Leave(Guid profileId)
    {
        var member = _members.FirstOrDefault(m => m.ProfileId == profileId)
            ?? throw new NotFoundException("You are not a member of this group.", "group.not_member");

        _members.Remove(member);

        if (_members.Count <= 1)
            return true;

        if (member.Role == GroupMemberRole.Captain)
        {
            var newCaptain = _members[Random.Shared.Next(_members.Count)];
            newCaptain.PromoteToCaptain();
        }

        UpdatedAt = DateTime.UtcNow;
        return false;
    }

    public void Update(string name, string? avatarUrl, Guid requestingProfileId)
    {
        EnsureIsCaptain(requestingProfileId);
        Name = name;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransferCaptaincy(Guid newCaptainProfileId, Guid requestingProfileId)
    {
        EnsureIsCaptain(requestingProfileId);
        if (requestingProfileId == newCaptainProfileId)
            throw new BusinessRuleException("You are already the captain of this group.", "group.already_captain");
        var currentCaptain = _members.First(m => m.Role == GroupMemberRole.Captain);
        var newCaptain = _members.FirstOrDefault(m => m.ProfileId == newCaptainProfileId)
            ?? throw new NotFoundException("The player is not a member of this group.", "group.member_not_found");
        currentCaptain.DemoteToMember();
        newCaptain.PromoteToCaptain();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(Guid requestingProfileId)
        => EnsureIsCaptain(requestingProfileId);

    private void EnsureIsCaptain(Guid profileId)
    {
        if (!_members.Any(m => m.ProfileId == profileId && m.Role == GroupMemberRole.Captain))
            throw new ForbiddenException("Only the captain can perform this action.", "group.captain_only");
    }
}
