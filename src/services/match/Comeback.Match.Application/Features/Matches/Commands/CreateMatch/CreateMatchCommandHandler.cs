namespace Comeback.Match.Application.Features.Matches.Commands.CreateMatch;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class CreateMatchCommandHandler : IRequestHandler<CreateMatchCommand, Guid>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly IMatchEventPublisher _publisher;
    private readonly IPlayerGroupClient _groupClient;
    private readonly IMatchJobScheduler _scheduler;

    public CreateMatchCommandHandler(
        IMatchRepository matches,
        IMatchUnitOfWork unitOfWork,
        IMatchEventPublisher publisher,
        IPlayerGroupClient groupClient,
        IMatchJobScheduler scheduler)
    {
        _matches = matches;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _groupClient = groupClient;
        _scheduler = scheduler;
    }

    public async Task<Guid> Handle(CreateMatchCommand cmd, CancellationToken ct)
    {
        var match = cmd.Type switch
        {
            MatchType.GroupMatch => await CreateGroupMatchAsync(cmd, ct),
            MatchType.GroupVsGroup => await CreateGroupVsGroupAsync(cmd, ct),
            _ => Match.Create(
                cmd.Title, cmd.Type, cmd.OrganizerUserId, cmd.OrganizerDisplayName,
                cmd.Location, cmd.StartsAt, cmd.DurationMinutes, cmd.PlayersPerTeam, cmd.MaxSubstitutes,
                cmd.Invitees.Select(i => (i.UserId, i.DisplayName))),
        };

        foreach (var guestName in cmd.GuestNames ?? [])
            match.AddGuest(cmd.OrganizerUserId, guestName);

        _matches.Add(match);
        await _unitOfWork.SaveChangesAsync(ct);

        // Schedule the result-entry reminder (15min after the match ends).
        var jobId = _scheduler.ScheduleResultReminder(match.Id, match.EndsAt.AddMinutes(15));
        match.SetResultReminderJobId(jobId);
        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var invitee in match.Participants.Where(p => !p.IsOrganizer && !p.IsGuest))
        {
            await _publisher.PublishAsync(new MatchInvitationSentIntegrationEvent(
                match.Id,
                match.Title,
                match.OrganizerUserId,
                cmd.OrganizerDisplayName,
                invitee.UserId,
                match.StartsAt,
                match.Location), ct);
        }

        if (match.Type == MatchType.GroupVsGroup && match.OpponentGroupCaptainUserId.HasValue)
        {
            await _publisher.PublishAsync(new GroupMatchInviteIntegrationEvent(
                match.Id,
                match.Title,
                match.OrganizerUserId,
                cmd.OrganizerDisplayName,
                match.GroupName!,
                match.OpponentGroupCaptainUserId.Value,
                match.StartsAt,
                match.Location), ct);
        }

        return match.Id;
    }

    private async Task<Match> CreateGroupMatchAsync(CreateMatchCommand cmd, CancellationToken ct)
    {
        if (!cmd.GroupId.HasValue)
            throw new BusinessRuleException("A group must be selected for a group match.", "match.group_required");

        var groupInfo = await _groupClient.GetGroupMatchInfoAsync(cmd.GroupId.Value, ct)
            ?? throw new NotFoundException("Group not found.", "group.not_found");

        return Match.CreateGroupMatch(
            cmd.Title, cmd.OrganizerUserId, cmd.OrganizerDisplayName,
            cmd.Location, cmd.StartsAt, cmd.DurationMinutes, cmd.PlayersPerTeam, cmd.MaxSubstitutes,
            groupInfo.GroupId, groupInfo.GroupName,
            groupInfo.Members.Select(m => (m.UserId, m.DisplayName)),
            cmd.Invitees.Select(i => (i.UserId, i.DisplayName)));
    }

    private async Task<Match> CreateGroupVsGroupAsync(CreateMatchCommand cmd, CancellationToken ct)
    {
        if (!cmd.GroupId.HasValue || !cmd.OpponentGroupId.HasValue)
            throw new BusinessRuleException("Both groups must be selected for a group-vs-group match.", "match.both_groups_required");
        if (cmd.GroupId.Value == cmd.OpponentGroupId.Value)
            throw new BusinessRuleException("The opponent group cannot be the same as your own group.", "match.opponent_group_same");

        var groupInfo = await _groupClient.GetGroupMatchInfoAsync(cmd.GroupId.Value, ct)
            ?? throw new NotFoundException("Group not found.", "group.not_found");
        var opponentInfo = await _groupClient.GetGroupMatchInfoAsync(cmd.OpponentGroupId.Value, ct)
            ?? throw new NotFoundException("Opponent group not found.", "group.opponent_not_found");

        var ownMemberIds = groupInfo.Members.Select(m => m.UserId).ToHashSet();
        if (opponentInfo.Members.Any(m => ownMemberIds.Contains(m.UserId)))
            throw new BusinessRuleException("The opponent group shares a member with your group.", "match.groups_share_member");

        return Match.CreateGroupVsGroup(
            cmd.Title, cmd.OrganizerUserId, cmd.OrganizerDisplayName,
            cmd.Location, cmd.StartsAt, cmd.DurationMinutes, cmd.PlayersPerTeam, cmd.MaxSubstitutes,
            groupInfo.GroupId, groupInfo.GroupName,
            groupInfo.Members.Select(m => (m.UserId, m.DisplayName)),
            cmd.Invitees.Select(i => (i.UserId, i.DisplayName)),
            opponentInfo.GroupId, opponentInfo.GroupName,
            opponentInfo.CaptainUserId, opponentInfo.CaptainDisplayName);
    }
}
