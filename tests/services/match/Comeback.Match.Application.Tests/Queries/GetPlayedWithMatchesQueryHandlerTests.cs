namespace Comeback.Match.Application.Tests.Queries;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.Features.Matches.Queries.GetPlayedWithMatches;
using Comeback.Match.Application.Tests.TestSupport;
using Comeback.Match.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;
using MatchEntity = Comeback.Match.Domain.Entities.Match;

public sealed class GetPlayedWithMatchesQueryHandlerTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly GetPlayedWithMatchesQueryHandler _sut;

    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    public GetPlayedWithMatchesQueryHandlerTests()
    {
        _sut = new GetPlayedWithMatchesQueryHandler(_matches);
    }

    private void GivenMatches(params MatchEntity[] matches)
        => _matches.GetByUserIdAsync(Me, Arg.Any<CancellationToken>()).Returns(matches.ToList());

    private async Task<int> CountFor(PlayedWithRelation relation)
    {
        var result = await _sut.Handle(new GetPlayedWithMatchesQuery(Me, Other, relation), CancellationToken.None);
        return result.Count;
    }

    [Fact]
    public async Task Relations_Partition_AllEqualsTeammatePlusOpponent()
    {
        // Together as teammates (both Home), together as opponents, and a match where Other did not play.
        var together = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home).WithPlayer(Other, MatchTeam.Home)
            .BuildWithResult(0, 0);
        var against = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home).WithPlayer(Other, MatchTeam.Away)
            .BuildWithResult(1, 0);
        var withoutOther = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home).WithPlayer(Guid.NewGuid(), MatchTeam.Away)
            .BuildWithResult(0, 0);

        GivenMatches(together, against, withoutOther);

        var all = await CountFor(PlayedWithRelation.All);
        var teammate = await CountFor(PlayedWithRelation.Teammate);
        var opponent = await CountFor(PlayedWithRelation.Opponent);

        teammate.Should().Be(1);
        opponent.Should().Be(1);
        all.Should().Be(teammate + opponent);
    }

    [Fact]
    public async Task ExcludesMatchWhereParticipantHasNoTeam()
    {
        // Me is accepted but has no team — the match counts in no mode (consistent with "played").
        var noTeamForMe = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.None)
            .WithPlayer(Other, MatchTeam.Home)
            .WithPlayer(Guid.NewGuid(), MatchTeam.Away)
            .BuildWithResult(0, 0);

        GivenMatches(noTeamForMe);

        (await CountFor(PlayedWithRelation.All)).Should().Be(0);
    }

    [Fact]
    public async Task IgnoresMatchesWithoutResult()
    {
        var scheduled = new MatchBuilder()
            .WithPlayer(Me, MatchTeam.Home).WithPlayer(Other, MatchTeam.Away)
            .BuildScheduled();

        GivenMatches(scheduled);

        (await CountFor(PlayedWithRelation.All)).Should().Be(0);
    }
}
