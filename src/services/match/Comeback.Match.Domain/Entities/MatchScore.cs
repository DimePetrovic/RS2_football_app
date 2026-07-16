namespace Comeback.Match.Domain.Entities;

using Comeback.Match.Domain.Enums;

public enum MatchResult
{
    Win,
    Draw,
    Loss,
}

/// <summary>Shared match-outcome logic - prevents "who won" from being computed independently in several places.</summary>
public static class MatchScore
{
    /// <summary>The winning side, or null when it is a draw.</summary>
    public static MatchTeam? Winner(int homeScore, int awayScore)
        => homeScore == awayScore
            ? null
            : homeScore > awayScore ? MatchTeam.Home : MatchTeam.Away;

    /// <summary>Outcome from the given team's perspective.</summary>
    public static MatchResult OutcomeFor(int homeScore, int awayScore, MatchTeam team)
    {
        var winner = Winner(homeScore, awayScore);
        if (winner is null) return MatchResult.Draw;
        return winner == team ? MatchResult.Win : MatchResult.Loss;
    }
}
