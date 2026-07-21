namespace Comeback.Social.Application.Tests.Common;

using Comeback.Social.Application.Common;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class PostEnricherTests
{
    private readonly IMatchDetailsClient _matchClient = Substitute.For<IMatchDetailsClient>();
    private readonly IProfileAvatarsClient _avatarsClient = Substitute.For<IProfileAvatarsClient>();
    private readonly PostEnricher _sut;

    private static readonly Guid MatchId = Guid.NewGuid();
    private static readonly Guid Scorer = Guid.NewGuid();
    private static readonly Guid Keeper = Guid.NewGuid();
    private static readonly Guid ScorerParticipant = Guid.NewGuid();
    private static readonly Guid KeeperParticipant = Guid.NewGuid();

    public PostEnricherTests()
    {
        _avatarsClient.GetPlayerInfosAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, ProfileBasicInfo>());
        _sut = new PostEnricher(_matchClient, _avatarsClient);
    }

    private Post GivenPostWithMatch(MatchDetailsInfo details, params MatchReviewInfo[] reviews)
    {
        _matchClient.GetMatchDetailsAsync(MatchId, Arg.Any<CancellationToken>()).Returns(details);
        _matchClient.GetReviewsAsync(MatchId, Arg.Any<CancellationToken>()).Returns(reviews.ToList());

        return Post.CreateMatchResultPost(
            MatchId, "Match", 1, 0,
            details.Participants.Select(p => (p.UserId, p.DisplayName)));
    }

    private static MatchDetailsInfo Details(
        IReadOnlyList<MatchParticipantInfo> participants,
        IReadOnlyList<MatchGoalInfo> goals)
        => new(participants, goals, "Field", DateTime.UtcNow, null, null);

    [Fact]
    public async Task Enrich_CountsGoalsAssistsAndOwnGoalsPerPlayer()
    {
        var details = Details(
            participants:
            [
                new(ScorerParticipant, Scorer, "Scorer", "Home", false, "Accepted"),
                new(KeeperParticipant, Keeper, "Keeper", "Away", true, "Accepted"),
            ],
            goals:
            [
                new(Scorer, "Home", IsOwnGoal: false, AssistUserId: Keeper),
                new(Scorer, "Home", IsOwnGoal: false, AssistUserId: null),
                new(Scorer, "Away", IsOwnGoal: true, AssistUserId: null),
            ]);
        var post = GivenPostWithMatch(details);

        var result = await _sut.EnrichAsync(post, Guid.NewGuid(), CancellationToken.None);

        var scorer = result.Players.Single(p => p.UserId == Scorer);
        scorer.Goals.Should().Be(2);      // dva regularna gola
        scorer.OwnGoals.Should().Be(1);   // autogol se broji odvojeno
        var keeper = result.Players.Single(p => p.UserId == Keeper);
        keeper.Assists.Should().Be(1);
    }

    [Fact]
    public async Task Enrich_AveragesRatingsAcrossReviews()
    {
        var details = Details(
            participants:
            [
                new(ScorerParticipant, Scorer, "Scorer", "Home", false, "Accepted"),
                new(KeeperParticipant, Keeper, "Keeper", "Away", false, "Accepted"),
            ],
            goals: []);
        // Two ratings for the same player: 8 and 6 -> average 7.0.
        var post = GivenPostWithMatch(details,
            new MatchReviewInfo(KeeperParticipant, ScorerParticipant, 8m, null, null, null, null, null),
            new MatchReviewInfo(Guid.NewGuid(), ScorerParticipant, 6m, null, null, null, null, "Solidno"));

        var result = await _sut.EnrichAsync(post, Guid.NewGuid(), CancellationToken.None);

        var scorer = result.Players.Single(p => p.UserId == Scorer);
        scorer.OverallRating.Should().Be(7.0m);
    }

    [Fact]
    public async Task Enrich_WhenMatchDetailsMissing_ReturnsNoPlayers()
    {
        _matchClient.GetMatchDetailsAsync(MatchId, Arg.Any<CancellationToken>())
            .Returns((MatchDetailsInfo?)null);
        var post = Post.CreateMatchResultPost(MatchId, "Match", 1, 0, []);

        var result = await _sut.EnrichAsync(post, Guid.NewGuid(), CancellationToken.None);

        result.Players.Should().BeEmpty();
    }
}
