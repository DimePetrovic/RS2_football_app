namespace Comeback.Match.Application.Tests.TestSupport;

using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MatchEntity = Comeback.Match.Domain.Entities.Match;

/// <summary>
/// Builds a match through the public domain API (create -> accept -> assign -> submit),
/// da testovi rade nad realnim stanjem umesto da zaobilaze invariante.
/// </summary>
public sealed class MatchBuilder
{
    public Guid OrganizerId { get; } = Guid.NewGuid();

    private readonly List<(Guid Id, MatchTeam Team, bool Captain)> _players = [];
    private readonly List<string> _guests = [];
    private int _playersPerTeam = 1;

    public MatchBuilder WithPlayer(Guid id, MatchTeam team, bool captain = false)
    {
        _players.Add((id, team, captain));
        return this;
    }

    public MatchBuilder WithGuest(string name)
    {
        _guests.Add(name);
        return this;
    }

    public MatchBuilder PlayersPerTeam(int n)
    {
        _playersPerTeam = n;
        return this;
    }

    /// <summary>A match that is just scheduled (no result), with teams assigned.</summary>
    public MatchEntity BuildScheduled()
    {
        var startsAt = DateTime.UtcNow.AddHours(-2);
        var match = MatchEntity.Create(
            "Test match", MatchType.Independent, OrganizerId, "Organizer",
            location: "Field", startsAt, durationMinutes: 60,
            playersPerTeam: _playersPerTeam, maxSubstitutes: 3,
            invitees: _players.Select(p => (p.Id, $"Player-{p.Id.ToString()[..4]}")));

        foreach (var p in _players)
        {
            match.RespondToInvitation(p.Id, accept: true);
            if (p.Team != MatchTeam.None)
                match.AssignPlayerToTeam(OrganizerId, p.Id, p.Team);
        }

        foreach (var name in _guests)
            match.AddGuest(OrganizerId, name);

        return match;
    }

    /// <summary>A match with an entered result; goals are auto-assigned to match the score.</summary>
    public MatchEntity BuildWithResult(int homeScore, int awayScore)
    {
        var match = BuildScheduled();

        // We look up a scorer only if a goal is needed for that side (0:0 does not require both teams).
        var goals = new List<GoalEntry>();
        if (homeScore > 0)
        {
            var firstHome = _players.First(p => p.Team == MatchTeam.Home).Id;
            for (var i = 0; i < homeScore; i++) goals.Add(new GoalEntry(firstHome, IsOwnGoal: false, AssistUserId: null));
        }
        if (awayScore > 0)
        {
            var firstAway = _players.First(p => p.Team == MatchTeam.Away).Id;
            for (var i = 0; i < awayScore; i++) goals.Add(new GoalEntry(firstAway, IsOwnGoal: false, AssistUserId: null));
        }

        match.SubmitResult(OrganizerId, homeScore, awayScore, goals);
        return match;
    }
}
