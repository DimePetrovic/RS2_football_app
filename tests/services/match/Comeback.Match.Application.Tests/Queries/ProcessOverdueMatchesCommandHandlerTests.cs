namespace Comeback.Match.Application.Tests.Queries;

using Comeback.BuildingBlocks.Domain.Events;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.Features.Matches.Commands.ProcessOverdueMatches;
using Comeback.Match.Application.Tests.TestSupport;
using Comeback.Match.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;
using MatchEntity = Comeback.Match.Domain.Entities.Match;

public sealed class ProcessOverdueMatchesCommandHandlerTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IMatchUnitOfWork _unitOfWork = Substitute.For<IMatchUnitOfWork>();
    private readonly IMatchEventPublisher _publisher = Substitute.For<IMatchEventPublisher>();
    private readonly ProcessOverdueMatchesCommandHandler _sut;
    private readonly List<IIntegrationEvent> _published = [];

    public ProcessOverdueMatchesCommandHandlerTests()
    {
        _publisher.PublishAsync(Arg.Do<IIntegrationEvent>(e => _published.Add(e)), Arg.Any<CancellationToken>());
        _sut = new ProcessOverdueMatchesCommandHandler(_matches, _unitOfWork, _publisher);
    }

    private void GivenSweep(params MatchEntity[] matches)
        => _matches.GetForOverdueSweepAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(matches.ToList());

    [Fact]
    public async Task ScheduledPastDeadline_BecomesOverdue_AndPublishesEvent()
    {
        // Match started 2h ago, no duration -> EndsAt (start + 2h) has just passed.
        var match = MatchEntity.Create(
            "Match", MatchType.Independent, Guid.NewGuid(), "Org",
            null, DateTime.UtcNow.AddHours(-2).AddMinutes(-1), null, 5, 3, invitees: []);
        GivenSweep(match);

        await _sut.Handle(new ProcessOverdueMatchesCommand(), CancellationToken.None);

        match.Status.Should().Be(MatchStatus.ResultOverdue);
        _published.OfType<MatchResultOverdueIntegrationEvent>()
            .Should().ContainSingle(e => e.MatchId == match.Id);
    }

    [Fact]
    public async Task ScheduledNotYetEnded_IsUntouched()
    {
        var match = MatchEntity.Create(
            "Match", MatchType.Independent, Guid.NewGuid(), "Org",
            null, DateTime.UtcNow.AddMinutes(-10), durationMinutes: 90, 5, 3, invitees: []);
        GivenSweep(match);

        await _sut.Handle(new ProcessOverdueMatchesCommand(), CancellationToken.None);

        match.Status.Should().Be(MatchStatus.Scheduled);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task Overdue_BecomesMissed_AndPublishesEvent()
    {
        var match = MatchEntity.Create(
            "Match", MatchType.Independent, Guid.NewGuid(), "Org",
            null, DateTime.UtcNow.AddDays(-1), 60, 5, 3, invitees: []);
        match.MarkResultOverdue();
        GivenSweep(match);

        await _sut.Handle(new ProcessOverdueMatchesCommand(), CancellationToken.None);

        match.Status.Should().Be(MatchStatus.Missed);
        _published.OfType<MatchMissedIntegrationEvent>()
            .Should().ContainSingle(e => e.MatchId == match.Id);
    }

    [Fact]
    public async Task WhenNothingChanges_DoesNotSaveOrPublish()
    {
        GivenSweep();

        await _sut.Handle(new ProcessOverdueMatchesCommand(), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _published.Should().BeEmpty();
    }
}
