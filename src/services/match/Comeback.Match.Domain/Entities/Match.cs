namespace Comeback.Match.Domain.Entities;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.Domain.Primitives;
using Comeback.Match.Domain.Enums;

public sealed class Match : AggregateRoot<Guid>
{
    private readonly List<MatchParticipant> _participants = [];
    private readonly List<MatchGoal> _goals = [];

    public string Title { get; private set; } = string.Empty;
    public MatchType Type { get; private set; }
    public MatchStatus Status { get; private set; }
    public Guid OrganizerUserId { get; private set; }
    public string? Location { get; private set; }
    public DateTime StartsAt { get; private set; }
    public int? DurationMinutes { get; private set; }
    public int PlayersPerTeam { get; private set; }
    public int MaxSubstitutes { get; private set; }
    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }
    public Guid? ResultSubmittedByUserId { get; private set; }
    public DateTime? ResultSubmittedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Guid? GroupId { get; private set; }
    public string? GroupName { get; private set; }
    public Guid? OpponentGroupId { get; private set; }
    public string? OpponentGroupName { get; private set; }
    public Guid? OpponentGroupCaptainUserId { get; private set; }
    public string? OpponentGroupCaptainDisplayName { get; private set; }
    public GroupInviteStatus? OpponentGroupInviteStatus { get; private set; }
    public Guid? SecondOrganizerUserId { get; private set; }

    /// <summary>Hangfire job that reminds the organizer to enter the result (15min after the match ends).</summary>
    public string? ResultReminderJobId { get; private set; }

    public IReadOnlyList<MatchParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyList<MatchGoal> Goals => _goals.AsReadOnly();

    /// <summary>Default match duration when none is entered (spec: 2h).</summary>
    public const int DefaultDurationMinutes = 120;

    /// <summary>Match end time (start + duration, or +2h if no duration is entered).</summary>
    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes ?? DefaultDurationMinutes);

    public bool HasResult => HomeScore.HasValue && AwayScore.HasValue;

    private Match() { }

    private Match(
        Guid id, string title, MatchType type, Guid organizerUserId,
        string? location, DateTime startsAt, int? durationMinutes,
        int playersPerTeam, int maxSubstitutes) : base(id)
    {
        Title = title;
        Type = type;
        Status = MatchStatus.Scheduled;
        OrganizerUserId = organizerUserId;
        Location = location;
        StartsAt = startsAt;
        DurationMinutes = durationMinutes;
        PlayersPerTeam = playersPerTeam;
        MaxSubstitutes = maxSubstitutes;
        CreatedAt = DateTime.UtcNow;
    }

    public static Match Create(
        string title,
        MatchType type,
        Guid organizerUserId,
        string organizerDisplayName,
        string? location,
        DateTime startsAt,
        int? durationMinutes,
        int playersPerTeam,
        int maxSubstitutes,
        IEnumerable<(Guid UserId, string DisplayName)> invitees)
    {
        var match = new Match(
            Guid.NewGuid(), title, type, organizerUserId,
            location, startsAt, durationMinutes, playersPerTeam, maxSubstitutes);

        match._participants.Add(MatchParticipant.CreateOrganizer(match.Id, organizerUserId, organizerDisplayName));
        match.AddInvitees(invitees, MatchTeam.None);

        return match;
    }

    public static Match CreateGroupMatch(
        string title,
        Guid organizerUserId,
        string organizerDisplayName,
        string? location,
        DateTime startsAt,
        int? durationMinutes,
        int playersPerTeam,
        int maxSubstitutes,
        Guid groupId,
        string groupName,
        IEnumerable<(Guid UserId, string DisplayName)> groupMembers,
        IEnumerable<(Guid UserId, string DisplayName)> individualInvitees)
    {
        var match = new Match(
            Guid.NewGuid(), title, MatchType.GroupMatch, organizerUserId,
            location, startsAt, durationMinutes, playersPerTeam, maxSubstitutes);

        match.GroupId = groupId;
        match.GroupName = groupName;

        match._participants.Add(MatchParticipant.CreateOrganizer(match.Id, organizerUserId, organizerDisplayName));
        match.AddInvitees(groupMembers, MatchTeam.None);
        match.AddInvitees(individualInvitees, MatchTeam.None);

        return match;
    }

    public static Match CreateGroupVsGroup(
        string title,
        Guid organizerUserId,
        string organizerDisplayName,
        string? location,
        DateTime startsAt,
        int? durationMinutes,
        int playersPerTeam,
        int maxSubstitutes,
        Guid groupId,
        string groupName,
        IEnumerable<(Guid UserId, string DisplayName)> groupMembers,
        IEnumerable<(Guid UserId, string DisplayName)> individualInvitees,
        Guid opponentGroupId,
        string opponentGroupName,
        Guid opponentGroupCaptainUserId,
        string opponentGroupCaptainDisplayName)
    {
        var match = new Match(
            Guid.NewGuid(), title, MatchType.GroupVsGroup, organizerUserId,
            location, startsAt, durationMinutes, playersPerTeam, maxSubstitutes);

        match.GroupId = groupId;
        match.GroupName = groupName;
        match.OpponentGroupId = opponentGroupId;
        match.OpponentGroupName = opponentGroupName;
        match.OpponentGroupCaptainUserId = opponentGroupCaptainUserId;
        match.OpponentGroupCaptainDisplayName = opponentGroupCaptainDisplayName;
        match.OpponentGroupInviteStatus = GroupInviteStatus.Pending;

        match._participants.Add(MatchParticipant.CreateOrganizer(match.Id, organizerUserId, organizerDisplayName));
        match.AddInvitees(groupMembers, MatchTeam.Home);
        match.AddInvitees(individualInvitees, MatchTeam.None);

        return match;
    }

    public void RespondToGroupInvite(
        Guid captainUserId, bool accept, IEnumerable<(Guid UserId, string DisplayName)> opponentGroupMembers)
    {
        if (Type != MatchType.GroupVsGroup)
            throw new BusinessRuleException("This match is not a group-vs-group match.", "match.not_group_vs_group");
        if (OpponentGroupCaptainUserId != captainUserId)
            throw new ForbiddenException("Only the invited captain can respond to this invite.", "match.captain_only");
        if (OpponentGroupInviteStatus != GroupInviteStatus.Pending)
            throw new BusinessRuleException("The invite has already been handled.", "match.invite_already_handled");
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You can respond to the invite only while the match is scheduled.", "match.invite_response_only_scheduled");

        if (!accept)
        {
            OpponentGroupInviteStatus = GroupInviteStatus.Declined;
            return;
        }

        OpponentGroupInviteStatus = GroupInviteStatus.Accepted;
        SecondOrganizerUserId = captainUserId;
        AddInvitees(opponentGroupMembers, MatchTeam.Away);
    }

    private void AddInvitees(IEnumerable<(Guid UserId, string DisplayName)> invitees, MatchTeam groupSide)
    {
        foreach (var (userId, displayName) in invitees)
        {
            if (userId == OrganizerUserId) continue;
            if (_participants.Any(p => p.UserId == userId && p.Status != MatchParticipantStatus.Removed)) continue;

            _participants.Add(MatchParticipant.CreateInvited(Id, userId, displayName, groupSide));
        }
    }

    public MatchParticipant RespondToInvitation(Guid userId, bool accept)
    {
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You can respond to the invite only while the match is scheduled.", "match.invite_response_only_scheduled");

        var participant = GetParticipantByUserId(userId);
        if (participant.Status != MatchParticipantStatus.Invited)
            throw new BusinessRuleException("The invite has already been accepted, declined, or is inactive.", "match.invite_not_active");

        if (accept)
        {
            participant.Accept();
            if (Type == MatchType.GroupVsGroup && participant.GroupSide != MatchTeam.None)
                participant.AssignToTeam(participant.GroupSide);
        }
        else
        {
            participant.Decline();
        }

        return participant;
    }

    public MatchParticipant Withdraw(Guid userId)
    {
        EnsureNotCancelled();
        var participant = GetParticipantByUserId(userId);
        if (participant.Status != MatchParticipantStatus.Accepted)
            throw new BusinessRuleException("You can withdraw only after accepting the invite.", "match.withdraw_requires_accept");
        if (participant.IsOrganizer || userId == SecondOrganizerUserId)
            throw new BusinessRuleException("The organizer cannot withdraw from the match.", "match.organizer_cannot_withdraw");

        participant.Withdraw();
        return participant;
    }

    public MatchParticipant AddGuest(Guid organizerUserId, string displayName)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You cannot add players in the current match status.", "match.add_player_wrong_status");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new BusinessRuleException("Guest name is required.", "match.guest_name_required");

        var guest = MatchParticipant.CreateGuest(Id, displayName.Trim());
        _participants.Add(guest);
        return guest;
    }

    /// <summary>A player applies via the public "player wanted" call and takes a free slot immediately (v1: no organizer approval).</summary>
    public MatchParticipant JoinViaPublicCall(Guid userId, string displayName)
    {
        EnsureNotCancelled();
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You can join only while the match is scheduled.", "match.join_only_scheduled");
        if (userId == OrganizerUserId || userId == SecondOrganizerUserId
            || _participants.Any(p => p.UserId == userId && p.Status is MatchParticipantStatus.Invited or MatchParticipantStatus.Accepted))
            throw new ConflictException("The player is already invited or participating in this match.", "match.player_already_in");

        var capacity = (PlayersPerTeam + 1) * 2 + MaxSubstitutes;
        var acceptedCount = _participants.Count(p => p.Status == MatchParticipantStatus.Accepted);
        if (acceptedCount >= capacity)
            throw new BusinessRuleException("The match is already full.", "match.full");

        var participant = MatchParticipant.CreateInvited(Id, userId, displayName);
        participant.Accept();
        _participants.Add(participant);
        return participant;
    }

    public void InvitePlayer(Guid organizerUserId, Guid userId, string displayName)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You cannot invite players in the current match status.", "match.invite_wrong_status");
        if (_participants.Any(p => p.UserId == userId && p.Status != MatchParticipantStatus.Removed))
            throw new ConflictException("The player is already invited or participating in this match.", "match.player_already_in");

        _participants.Add(MatchParticipant.CreateInvited(Id, userId, displayName));
    }

    /// <summary>The organizer removes a player (surplus/unassigned or any participant) from the match.</summary>
    public MatchParticipant RemoveParticipant(Guid organizerUserId, Guid targetUserId)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You can remove players only while the match is scheduled.", "match.remove_only_scheduled");
        if (targetUserId == OrganizerUserId || targetUserId == SecondOrganizerUserId)
            throw new BusinessRuleException("The organizer cannot be removed from the match.", "match.organizer_cannot_be_removed");

        var participant = _participants.FirstOrDefault(
                p => p.UserId == targetUserId && p.Status != MatchParticipantStatus.Removed)
            ?? throw new NotFoundException("The player is not a participant of this match.", "match.participant_not_found");

        participant.Remove();
        return participant;
    }

    public void UpdateDetails(
        Guid organizerUserId, string title, string? location, DateTime startsAt, int? durationMinutes)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("Match details can be changed only while the match is scheduled.", "match.update_only_scheduled");

        Title = title;
        Location = location;
        StartsAt = startsAt;
        DurationMinutes = durationMinutes;
    }

    public void SubmitResult(Guid userId, int homeScore, int awayScore, IReadOnlyList<GoalEntry> goals)
    {
        EnsureOrganizer(userId);
        EnsureNotCancelled();
        // The result can be entered while the match is scheduled or while entry is overdue (before it becomes missed).
        if (Status != MatchStatus.Scheduled && Status != MatchStatus.ResultOverdue)
            throw new BusinessRuleException("The result can only be entered for scheduled matches.", "match.result_only_scheduled");

        if (DateTime.UtcNow < EndsAt)
            throw new BusinessRuleException("The result can be entered only after the match ends.", "match.result_after_end");

        var requiredPlayers = (PlayersPerTeam + 1) * 2;
        var acceptedCount = _participants.Count(p => p.Status == MatchParticipantStatus.Accepted);
        if (requiredPlayers - acceptedCount > 2)
            throw new BusinessRuleException("More than 2 required players are missing, so the result cannot be entered.", "match.result_not_enough_players");

        var newGoals = new List<MatchGoal>();
        foreach (var g in goals)
        {
            var scorer = _participants.FirstOrDefault(
                p => p.UserId == g.ScorerUserId && p.Status == MatchParticipantStatus.Accepted)
                ?? throw new NotFoundException("The scorer is not an accepted participant.", "match.scorer_not_participant");
            if (scorer.Team == MatchTeam.None)
                throw new BusinessRuleException("The scorer must be assigned to a team.", "match.scorer_no_team");

            var scoringTeam = g.IsOwnGoal
                ? (scorer.Team == MatchTeam.Home ? MatchTeam.Away : MatchTeam.Home)
                : scorer.Team;

            string? assistDisplayName = null;
            if (g.AssistUserId.HasValue)
            {
                if (g.IsOwnGoal)
                    throw new BusinessRuleException("An own goal cannot have an assist.", "match.own_goal_no_assist");

                var assist = _participants.FirstOrDefault(
                    p => p.UserId == g.AssistUserId.Value && p.Status == MatchParticipantStatus.Accepted)
                    ?? throw new NotFoundException("The assisting player is not an accepted participant.", "match.assist_not_participant");
                if (assist.Team != scorer.Team)
                    throw new BusinessRuleException("The assisting player must be on the same team as the scorer.", "match.assist_wrong_team");

                assistDisplayName = assist.DisplayName;
            }

            newGoals.Add(MatchGoal.Create(
                Id, scorer.UserId, scorer.DisplayName, scoringTeam, g.IsOwnGoal, g.AssistUserId, assistDisplayName));
        }

        var homeGoals = newGoals.Count(g => g.ScoringTeam == MatchTeam.Home);
        var awayGoals = newGoals.Count(g => g.ScoringTeam == MatchTeam.Away);
        if (homeGoals != homeScore || awayGoals != awayScore)
            throw new BusinessRuleException("The number of goals does not match the entered result.", "match.goals_mismatch_score");

        _goals.AddRange(newGoals);

        HomeScore = homeScore;
        AwayScore = awayScore;
        ResultSubmittedByUserId = userId;
        ResultSubmittedAt = DateTime.UtcNow;
        Status = MatchStatus.ResultSubmitted;
        ResultReminderJobId = null;
    }

    public void SetResultReminderJobId(string? jobId) => ResultReminderJobId = jobId;

    /// <summary>Daily sweep: a scheduled match past its result deadline -> entry overdue.</summary>
    public void MarkResultOverdue()
    {
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("Only a scheduled match can move to result-overdue.", "match.overdue_requires_scheduled");
        Status = MatchStatus.ResultOverdue;
        ResultReminderJobId = null;
    }

    /// <summary>Daily sweep: a match whose result is still overdue -> missed.</summary>
    public void MarkMissed()
    {
        if (Status != MatchStatus.ResultOverdue)
            throw new BusinessRuleException("Only a result-overdue match can be marked missed.", "match.missed_requires_overdue");
        Status = MatchStatus.Missed;
    }

    public void Cancel(Guid userId)
    {
        EnsureOrganizer(userId);
        if (Status != MatchStatus.Scheduled)
            throw new BusinessRuleException("You cannot cancel the match in the current status.", "match.cancel_wrong_status");

        Status = MatchStatus.Cancelled;
        ResultReminderJobId = null;
    }

    public void AssignPlayerToTeam(Guid organizerUserId, Guid targetUserId, MatchTeam team)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        var participant = GetAcceptedParticipant(targetUserId);
        participant.AssignToTeam(team);
    }

    public void RandomizeTeams(Guid organizerUserId)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        var accepted = _participants.Where(p => p.Status == MatchParticipantStatus.Accepted).ToList();
        var shuffled = accepted.OrderBy(_ => Random.Shared.Next()).ToList();
        for (int i = 0; i < shuffled.Count; i++)
            shuffled[i].AssignToTeam(i % 2 == 0 ? MatchTeam.Home : MatchTeam.Away);
    }

    public void RandomizeTeamsWithCaptains(Guid organizerUserId, Guid homeCaptainId, Guid awayCaptainId)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        if (homeCaptainId == awayCaptainId)
            throw new BusinessRuleException("The two team captains cannot be the same player.", "match.captains_must_differ");

        var accepted = _participants.Where(p => p.Status == MatchParticipantStatus.Accepted).ToList();
        var homeCaptain = accepted.FirstOrDefault(p => p.UserId == homeCaptainId)
            ?? throw new NotFoundException("The home captain is not an accepted participant.", "match.home_captain_not_participant");
        var awayCaptain = accepted.FirstOrDefault(p => p.UserId == awayCaptainId)
            ?? throw new NotFoundException("The away captain is not an accepted participant.", "match.away_captain_not_participant");

        foreach (var p in accepted) p.AssignToTeam(MatchTeam.None, isCaptain: false);

        homeCaptain.AssignToTeam(MatchTeam.Home, isCaptain: true);
        awayCaptain.AssignToTeam(MatchTeam.Away, isCaptain: true);

        var rest = accepted
            .Where(p => p.UserId != homeCaptainId && p.UserId != awayCaptainId)
            .OrderBy(_ => Random.Shared.Next()).ToList();

        for (int i = 0; i < rest.Count; i++)
            rest[i].AssignToTeam(i % 2 == 0 ? MatchTeam.Home : MatchTeam.Away);
    }

    public void BalanceTeams(Guid organizerUserId, IReadOnlyList<(Guid UserId, int Rating)> ratings)
    {
        EnsureOrganizer(organizerUserId);
        EnsureNotCancelled();
        var ratingMap = ratings.ToDictionary(r => r.UserId, r => r.Rating);
        var accepted = _participants
            .Where(p => p.Status == MatchParticipantStatus.Accepted)
            .OrderByDescending(p => ratingMap.TryGetValue(p.UserId, out var r) ? r : 0)
            .ToList();

        for (int i = 0; i < accepted.Count; i++)
            accepted[i].AssignToTeam(i % 2 == 0 ? MatchTeam.Home : MatchTeam.Away);
    }

    private void EnsureOrganizer(Guid userId)
    {
        if (OrganizerUserId != userId && SecondOrganizerUserId != userId)
            throw new ForbiddenException("Only the organizer can perform this action.", "match.organizer_only");
    }

    private MatchParticipant GetAcceptedParticipant(Guid userId)
        => _participants.FirstOrDefault(p => p.UserId == userId && p.Status == MatchParticipantStatus.Accepted)
           ?? throw new NotFoundException("The player is not an accepted participant.", "match.player_not_accepted");

    private MatchParticipant GetParticipantByUserId(Guid userId)
        => _participants.FirstOrDefault(p => p.UserId == userId)
           ?? throw new NotFoundException("You are not a participant of this match.", "match.not_participant");

    private void EnsureNotCancelled()
    {
        if (Status == MatchStatus.Cancelled)
            throw new BusinessRuleException("The match has been cancelled.", "match.cancelled");
    }
}
