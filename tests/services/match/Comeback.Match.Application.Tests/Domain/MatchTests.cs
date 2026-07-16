namespace Comeback.Match.Application.Tests.Domain;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Tests.TestSupport;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using FluentAssertions;
using Xunit;

public sealed class MatchTests
{
    [Fact]
    public void AddGuest_CreatesAcceptedGuestWithoutTeam()
    {
        var builder = new MatchBuilder();
        var match = builder.BuildScheduled();

        var guest = match.AddGuest(builder.OrganizerId, "Marko sa posla");

        guest.IsGuest.Should().BeTrue();
        guest.Status.Should().Be(MatchParticipantStatus.Accepted);
        guest.Team.Should().Be(MatchTeam.None);
        guest.DisplayName.Should().Be("Marko sa posla");
    }

    [Fact]
    public void AddGuest_ByNonOrganizer_IsForbidden()
    {
        var match = new MatchBuilder().BuildScheduled();

        var act = () => match.AddGuest(Guid.NewGuid(), "Neko");

        act.Should().Throw<ForbiddenException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddGuest_WithBlankName_IsRejected(string name)
    {
        var builder = new MatchBuilder();
        var match = builder.BuildScheduled();

        var act = () => match.AddGuest(builder.OrganizerId, name);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_SkipsDuplicateInvitees()
    {
        var duplicate = Guid.NewGuid();
        var match = Comeback.Match.Domain.Entities.Match.Create(
            "Match", MatchType.Independent, Guid.NewGuid(), "Org",
            null, DateTime.UtcNow.AddDays(1), 60, 5, 3,
            invitees: [(duplicate, "Isti"), (duplicate, "Isti opet")]);

        match.Participants.Count(p => p.UserId == duplicate).Should().Be(1);
    }

    [Fact]
    public void SubmitResult_WhenGoalsDoNotMatchScore_IsRejected()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var builder = new MatchBuilder()
            .WithPlayer(home, MatchTeam.Home)
            .WithPlayer(away, MatchTeam.Away);
        var match = builder.BuildScheduled();

        // Reported score 2:0, but only one goal was entered.
        var act = () => match.SubmitResult(builder.OrganizerId, 2, 0,
            [new GoalEntry(home, IsOwnGoal: false, AssistUserId: null)]);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SubmitResult_OwnGoalWithAssist_IsRejected()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var builder = new MatchBuilder()
            .WithPlayer(home, MatchTeam.Home)
            .WithPlayer(away, MatchTeam.Away);
        var match = builder.BuildScheduled();

        var act = () => match.SubmitResult(builder.OrganizerId, 0, 1,
            [new GoalEntry(home, IsOwnGoal: true, AssistUserId: away)]);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SubmitResult_OwnGoalCountsForOpposingTeam()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var builder = new MatchBuilder()
            .WithPlayer(home, MatchTeam.Home)
            .WithPlayer(away, MatchTeam.Away);
        var match = builder.BuildScheduled();

        // Own goal by a home player -> goal for the away side (0:1).
        match.SubmitResult(builder.OrganizerId, 0, 1,
            [new GoalEntry(home, IsOwnGoal: true, AssistUserId: null)]);

        match.HomeScore.Should().Be(0);
        match.AwayScore.Should().Be(1);
        match.Status.Should().Be(MatchStatus.ResultSubmitted);
    }

    [Fact]
    public void SubmitResult_ByNonOrganizer_IsForbidden()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var match = new MatchBuilder()
            .WithPlayer(home, MatchTeam.Home)
            .WithPlayer(away, MatchTeam.Away)
            .BuildScheduled();

        var act = () => match.SubmitResult(home, 0, 0, []);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void Withdraw_ByOrganizer_IsRejected()
    {
        var builder = new MatchBuilder();
        var match = builder.BuildScheduled();

        var act = () => match.Withdraw(builder.OrganizerId);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RemoveParticipant_ByOrganizer_MarksParticipantRemoved()
    {
        var player = Guid.NewGuid();
        var builder = new MatchBuilder().WithPlayer(player, MatchTeam.None);
        var match = builder.BuildScheduled();

        match.RemoveParticipant(builder.OrganizerId, player);

        match.Participants.Single(p => p.UserId == player)
            .Status.Should().Be(MatchParticipantStatus.Removed);
    }

    [Fact]
    public void RemoveParticipant_ByNonOrganizer_IsForbidden()
    {
        var player = Guid.NewGuid();
        var match = new MatchBuilder().WithPlayer(player, MatchTeam.None).BuildScheduled();

        var act = () => match.RemoveParticipant(player, player);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void RemoveParticipant_TargetingOrganizer_IsRejected()
    {
        var builder = new MatchBuilder();
        var match = builder.BuildScheduled();

        var act = () => match.RemoveParticipant(builder.OrganizerId, builder.OrganizerId);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EndsAt_WithoutDuration_DefaultsToTwoHours()
    {
        var startsAt = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc);
        var match = Comeback.Match.Domain.Entities.Match.Create(
            "Match", MatchType.Independent, Guid.NewGuid(), "Org",
            null, startsAt, durationMinutes: null, playersPerTeam: 5, maxSubstitutes: 3,
            invitees: []);

        match.EndsAt.Should().Be(startsAt.AddHours(2));
    }

    [Fact]
    public void MarkResultOverdue_FromScheduled_TransitionsAndClearsJob()
    {
        var match = new MatchBuilder().BuildScheduled();
        match.SetResultReminderJobId("job-1");

        match.MarkResultOverdue();

        match.Status.Should().Be(MatchStatus.ResultOverdue);
        match.ResultReminderJobId.Should().BeNull();
    }

    [Fact]
    public void MarkMissed_FromResultOverdue_TransitionsToMissed()
    {
        var match = new MatchBuilder().BuildScheduled();
        match.MarkResultOverdue();

        match.MarkMissed();

        match.Status.Should().Be(MatchStatus.Missed);
    }

    [Fact]
    public void MarkMissed_WhenNotOverdue_IsRejected()
    {
        var match = new MatchBuilder().BuildScheduled();

        var act = () => match.MarkMissed();

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SubmitResult_WhenResultOverdue_IsAllowed()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var builder = new MatchBuilder()
            .WithPlayer(home, MatchTeam.Home)
            .WithPlayer(away, MatchTeam.Away);
        var match = builder.BuildScheduled();
        match.MarkResultOverdue();

        match.SubmitResult(builder.OrganizerId, 1, 0,
            [new GoalEntry(home, IsOwnGoal: false, AssistUserId: null)]);

        match.Status.Should().Be(MatchStatus.ResultSubmitted);
        match.HomeScore.Should().Be(1);
    }

    [Fact]
    public void Cancel_SetsStatusToCancelled()
    {
        var builder = new MatchBuilder();
        var match = builder.BuildScheduled();

        match.Cancel(builder.OrganizerId);

        match.Status.Should().Be(MatchStatus.Cancelled);
    }
}
