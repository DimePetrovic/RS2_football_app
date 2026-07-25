namespace Comeback.Match.Application.Tests.Commands;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.Features.Matches.Commands.UpdateMatchDetails;
using Comeback.Match.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;
using MatchEntity = Comeback.Match.Domain.Entities.Match;

public sealed class UpdateMatchDetailsCommandHandlerTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IMatchUnitOfWork _unitOfWork = Substitute.For<IMatchUnitOfWork>();
    private readonly IMatchEventPublisher _publisher = Substitute.For<IMatchEventPublisher>();
    private readonly IMatchJobScheduler _scheduler = Substitute.For<IMatchJobScheduler>();
    private readonly UpdateMatchDetailsCommandHandler _sut;

    public UpdateMatchDetailsCommandHandlerTests()
    {
        _sut = new UpdateMatchDetailsCommandHandler(_matches, _unitOfWork, _publisher, _scheduler);
    }

    private UpdateMatchDetailsCommand Command(MatchEntity match, DateTime startsAt, int? duration) =>
        new(match.Id, match.OrganizerUserId, "Novi naziv", "Nova lokacija", startsAt, duration);

    [Fact]
    public async Task Handle_WhenEndTimeUnchanged_DoesNotTouchScheduler()
    {
        var match = new MatchBuilder().BuildScheduled();
        _matches.GetByIdWithParticipantsAsync(match.Id, Arg.Any<CancellationToken>()).Returns(match);

        // Same start + duration => EndsAt unchanged => a pure title/location edit.
        await _sut.Handle(Command(match, match.StartsAt, 60), CancellationToken.None);

        _scheduler.DidNotReceive().CancelJob(Arg.Any<string?>());
        _scheduler.DidNotReceive().ScheduleResultReminder(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WhenEditedMatchEndsInThePast_DoesNotScheduleAnInstantReminder()
    {
        var match = new MatchBuilder().BuildScheduled(); // already ended (StartsAt = now - 2h)
        _matches.GetByIdWithParticipantsAsync(match.Id, Arg.Any<CancellationToken>()).Returns(match);

        // Move the (already past) start further back: EndsAt changes but stays in the past.
        await _sut.Handle(Command(match, match.StartsAt.AddHours(-1), 60), CancellationToken.None);

        _scheduler.Received(1).CancelJob(Arg.Any<string?>());
        _scheduler.DidNotReceive().ScheduleResultReminder(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WhenEndTimeMovedToFuture_ReschedulesReminder()
    {
        var match = new MatchBuilder().BuildScheduled();
        _matches.GetByIdWithParticipantsAsync(match.Id, Arg.Any<CancellationToken>()).Returns(match);

        await _sut.Handle(Command(match, DateTime.UtcNow.AddHours(1), 60), CancellationToken.None);

        _scheduler.Received(1).CancelJob(Arg.Any<string?>());
        _scheduler.Received(1).ScheduleResultReminder(match.Id, Arg.Any<DateTimeOffset>());
    }
}
