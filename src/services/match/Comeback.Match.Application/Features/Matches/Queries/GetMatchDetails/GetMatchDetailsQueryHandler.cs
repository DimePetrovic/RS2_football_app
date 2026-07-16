namespace Comeback.Match.Application.Features.Matches.Queries.GetMatchDetails;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class GetMatchDetailsQueryHandler : IRequestHandler<GetMatchDetailsQuery, MatchDetailResponse>
{
    private readonly IMatchRepository _matches;
    private readonly IPlayerInfoClient _playerInfo;

    public GetMatchDetailsQueryHandler(IMatchRepository matches, IPlayerInfoClient playerInfo)
    {
        _matches = matches;
        _playerInfo = playerInfo;
    }

    public async Task<MatchDetailResponse> Handle(GetMatchDetailsQuery query, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(query.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        // Players are always shown with avatar and username; guests have no account.
        var playerInfos = (await _playerInfo.GetPlayerInfosAsync(
                match.Participants.Where(p => !p.IsGuest).Select(p => p.UserId), ct))
            .ToDictionary(i => i.UserId);

        var myXpChange = CalculateMyXpChange(match, query.RequestingUserId);

        var teamCapacity = match.PlayersPerTeam + 1;
        var benchIds = new HashSet<Guid>();
        foreach (var team in new[] { MatchTeam.Home, MatchTeam.Away })
        {
            var teamPlayers = match.Participants
                .Where(p => p.Team == team)
                .OrderBy(p => p.TeamAssignedAt)
                .ToList();
            foreach (var p in teamPlayers.Skip(teamCapacity))
                benchIds.Add(p.Id);
        }

        return new MatchDetailResponse(
            match.Id,
            match.Title,
            match.Type.ToString(),
            match.Status.ToString(),
            match.OrganizerUserId,
            match.Location,
            match.StartsAt,
            match.DurationMinutes,
            match.PlayersPerTeam,
            match.MaxSubstitutes,
            match.HomeScore,
            match.AwayScore,
            match.ResultSubmittedAt,
            match.CreatedAt,
            match.Participants.Select(p => new ParticipantResponse(
                p.Id,
                p.UserId,
                playerInfos.TryGetValue(p.UserId, out var infoName) && !string.IsNullOrWhiteSpace(infoName.DisplayName)
                    ? infoName.DisplayName!
                    : p.DisplayName,
                p.IsOrganizer,
                p.IsCaptain,
                p.Team.ToString(),
                p.Status.ToString(),
                p.InvitedAt,
                p.RespondedAt,
                benchIds.Contains(p.Id),
                p.IsGuest,
                playerInfos.TryGetValue(p.UserId, out var info) ? info.Username : null,
                playerInfos.TryGetValue(p.UserId, out var info2) ? info2.AvatarUrl : null,
                playerInfos.TryGetValue(p.UserId, out var info3) ? info3.Nationality : null)).ToList(),
            match.Goals.Select(g => new GoalResponse(
                g.ScorerUserId,
                g.ScorerDisplayName,
                g.ScoringTeam.ToString(),
                g.IsOwnGoal,
                g.AssistUserId,
                g.AssistDisplayName)).ToList(),
            match.GroupId,
            match.GroupName,
            match.OpponentGroupId,
            match.OpponentGroupName,
            match.OpponentGroupCaptainUserId,
            match.OpponentGroupCaptainDisplayName,
            match.OpponentGroupInviteStatus?.ToString(),
            match.SecondOrganizerUserId,
            myXpChange);
    }

    // XP the player earned in this match — only after a result is entered and for a player on a team.
    // Values come from the same rules as the awarding in the Rating service (MatchXpRules).
    private static int? CalculateMyXpChange(Domain.Entities.Match match, Guid? userId)
    {
        if (userId is null || !match.HomeScore.HasValue || !match.AwayScore.HasValue)
            return null;

        var me = match.Participants.FirstOrDefault(
            p => p.UserId == userId && p.Team != MatchTeam.None
              && p.Status == MatchParticipantStatus.Accepted);
        if (me is null) return null;

        var outcome = MatchScore.OutcomeFor(match.HomeScore.Value, match.AwayScore.Value, me.Team);
        return MatchXpRules.Calculate(
            isWinner: outcome == MatchResult.Win,
            isDraw: outcome == MatchResult.Draw,
            isCaptain: me.IsCaptain);
    }
}
