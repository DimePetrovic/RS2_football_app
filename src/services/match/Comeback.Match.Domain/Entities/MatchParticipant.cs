namespace Comeback.Match.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Match.Domain.Enums;

public sealed class MatchParticipant : Entity<Guid>
{
    public Guid MatchId { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsOrganizer { get; private set; }
    public MatchParticipantStatus Status { get; private set; }
    public MatchTeam Team { get; private set; }
    public bool IsCaptain { get; private set; }
    public DateTime InvitedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public DateTime? TeamAssignedAt { get; private set; }
    public MatchTeam GroupSide { get; private set; }
    public bool IsGuest { get; private set; }

    private MatchParticipant() { }

    private MatchParticipant(
        Guid id, Guid matchId, Guid userId, string displayName,
        bool isOrganizer, MatchParticipantStatus status, MatchTeam groupSide,
        bool isGuest = false) : base(id)
    {
        MatchId = matchId;
        UserId = userId;
        DisplayName = displayName;
        IsOrganizer = isOrganizer;
        Status = status;
        Team = MatchTeam.None;
        IsCaptain = false;
        InvitedAt = DateTime.UtcNow;
        RespondedAt = isOrganizer || isGuest ? DateTime.UtcNow : null;
        GroupSide = groupSide;
        IsGuest = isGuest;
    }

    internal static MatchParticipant CreateOrganizer(Guid matchId, Guid userId, string displayName)
        => new(Guid.NewGuid(), matchId, userId, displayName, true, MatchParticipantStatus.Accepted, MatchTeam.None);

    internal static MatchParticipant CreateInvited(Guid matchId, Guid userId, string displayName, MatchTeam groupSide = MatchTeam.None)
        => new(Guid.NewGuid(), matchId, userId, displayName, false, MatchParticipantStatus.Invited, groupSide);

    // A guest has no account — UserId is generated and only serves internal links (goals, teams).
    internal static MatchParticipant CreateGuest(Guid matchId, string displayName)
        => new(Guid.NewGuid(), matchId, Guid.NewGuid(), displayName, false, MatchParticipantStatus.Accepted, MatchTeam.None, isGuest: true);

    internal void Accept()
    {
        Status = MatchParticipantStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    internal void Decline()
    {
        Status = MatchParticipantStatus.Declined;
        RespondedAt = DateTime.UtcNow;
    }

    internal void Withdraw()
    {
        Status = MatchParticipantStatus.Withdrawn;
        RespondedAt = DateTime.UtcNow;
    }

    internal void Remove()
    {
        Status = MatchParticipantStatus.Removed;
        RespondedAt = DateTime.UtcNow;
    }

    internal void AssignToTeam(MatchTeam team, bool isCaptain = false)
    {
        Team = team;
        IsCaptain = isCaptain;
        TeamAssignedAt = team == MatchTeam.None ? null : DateTime.UtcNow;
    }
}
