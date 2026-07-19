namespace Comeback.Match.Application.Tests.Queries;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.Features.Matches.Queries.GetPlayerStats;
using Comeback.Match.Application.Tests.TestSupport;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;
using MatchEntity = Comeback.Match.Domain.Entities.Match;

public sealed class GetPlayerStatsQueryHandlerTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IPlayerInfoClient _playerInfo = Substitute.For<IPlayerInfoClient>();
    private readonly GetPlayerStatsQueryHandler _sut;

    private static readonly Guid Me = Guid.NewGuid();

    public GetPlayerStatsQueryHandlerTests()
    {
        _playerInfo.GetPlayerInfosAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<PlayerInfo>());
        _sut = new GetPlayerStatsQueryHandler(_matches, _playerInfo);
    }

    private void GivenMatches(params MatchEntity[] matches)
        => _matches.GetByUserIdAsync(Me, Arg.Any<CancellationToken>()).Returns(matches.ToList());

    private MatchEntity MatchWhereMe(MatchTeam team, int home, int away)
        => new MatchBuilder()
            .WithPlayer(Me, team)
            .WithPlayer(Guid.NewGuid(), team == MatchTeam.Home ? MatchTeam.Away : MatchTeam.Home)
            .BuildWithResult(home, away);

    [Fact]
    public async Task CountsWinsDrawsLossesFromMyPerspective()
    {
        GivenMatches(
            MatchWhereMe(MatchTeam.Home, 2, 0),  // win
            MatchWhereMe(MatchTeam.Home, 1, 1),  // draw
            MatchWhereMe(MatchTeam.Away, 3, 0));  // loss (Me is Away)

        var stats = await _sut.Handle(new GetPlayerStatsQuery(Me), CancellationToken.None);

        stats.PlayedCount.Should().Be(3);
        stats.Wins.Should().Be(1);
        stats.Draws.Should().Be(1);
        stats.Losses.Should().Be(1);
    }

    [Fact]
    public async Task DoesNotCountMatchWithoutResult()
    {
        var scheduled = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home)
            .WithPlayer(Guid.NewGuid(), MatchTeam.Away)
            .BuildScheduled();

        GivenMatches(scheduled);

        var stats = await _sut.Handle(new GetPlayerStatsQuery(Me), CancellationToken.None);

        stats.PlayedCount.Should().Be(0);
    }

    [Fact]
    public async Task DoesNotCountMatchWhereIHaveNoTeam()
    {
        var noTeam = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.None)
            .WithPlayer(Guid.NewGuid(), MatchTeam.Home)
            .WithPlayer(Guid.NewGuid(), MatchTeam.Away)
            .BuildWithResult(1, 0);

        GivenMatches(noTeam);

        var stats = await _sut.Handle(new GetPlayerStatsQuery(Me), CancellationToken.None);

        stats.PlayedCount.Should().Be(0);
    }

    [Fact]
    public async Task GoalsExcludeOwnGoals()
    {
        var opponent = Guid.NewGuid();
        var builder = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home)
            .WithPlayer(opponent, MatchTeam.Away);
        var match = builder.BuildScheduled();

        // Me daje regularan gol; zatim autogol koji ide protivniku.
        match.SubmitResult(builder.OrganizerId, 1, 1,
        [
            new GoalEntry(Me, IsOwnGoal: false, AssistUserId: null),
            new GoalEntry(Me, IsOwnGoal: true, AssistUserId: null),
        ]);

        GivenMatches(match);

        var stats = await _sut.Handle(new GetPlayerStatsQuery(Me), CancellationToken.None);

        stats.Goals.Should().Be(1);
    }

    [Fact]
    public async Task OrganizedCount_ReflectsMatchesIOrganized()
    {
        var mine = MatchWhereMe(MatchTeam.Home, 1, 0);
        var someoneElses = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home)
            .WithPlayer(Guid.NewGuid(), MatchTeam.Away)
            .BuildWithResult(0, 0);
        // U 'mine' Me nije organizator (organizator je builder.OrganizerId), pa OrganizedCount = 0.

        GivenMatches(mine, someoneElses);

        var stats = await _sut.Handle(new GetPlayerStatsQuery(Me), CancellationToken.None);

        stats.OrganizedCount.Should().Be(0);
    }
}
