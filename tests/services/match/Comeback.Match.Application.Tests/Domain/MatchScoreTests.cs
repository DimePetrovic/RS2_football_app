namespace Comeback.Match.Application.Tests.Domain;

using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using FluentAssertions;
using Xunit;

public sealed class MatchScoreTests
{
    [Theory]
    [InlineData(2, 1, MatchTeam.Home)]
    [InlineData(0, 3, MatchTeam.Away)]
    public void Winner_ReturnsSideWithMoreGoals(int home, int away, MatchTeam expected)
    {
        MatchScore.Winner(home, away).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    public void Winner_WhenEqual_ReturnsNull(int home, int away)
    {
        MatchScore.Winner(home, away).Should().BeNull();
    }

    [Theory]
    [InlineData(3, 1, MatchTeam.Home, MatchResult.Win)]
    [InlineData(3, 1, MatchTeam.Away, MatchResult.Loss)]
    [InlineData(1, 4, MatchTeam.Home, MatchResult.Loss)]
    [InlineData(1, 4, MatchTeam.Away, MatchResult.Win)]
    [InlineData(2, 2, MatchTeam.Home, MatchResult.Draw)]
    [InlineData(2, 2, MatchTeam.Away, MatchResult.Draw)]
    public void OutcomeFor_ReturnsResultFromTeamPerspective(
        int home, int away, MatchTeam team, MatchResult expected)
    {
        MatchScore.OutcomeFor(home, away, team).Should().Be(expected);
    }

    [Fact]
    public void OutcomeFor_IsSymmetric_HomeWinIsAwayLoss()
    {
        var home = MatchScore.OutcomeFor(2, 0, MatchTeam.Home);
        var away = MatchScore.OutcomeFor(2, 0, MatchTeam.Away);

        home.Should().Be(MatchResult.Win);
        away.Should().Be(MatchResult.Loss);
    }
}
