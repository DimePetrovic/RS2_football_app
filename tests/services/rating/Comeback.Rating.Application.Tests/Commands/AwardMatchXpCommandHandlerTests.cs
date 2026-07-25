namespace Comeback.Rating.Application.Tests.Commands;

using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Application.Features.Xp.Commands.AwardMatchXp;
using Comeback.Rating.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class AwardMatchXpCommandHandlerTests
{
    private readonly IPlayerXpRepository _repository = Substitute.For<IPlayerXpRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AwardMatchXpCommandHandler _sut;

    private static readonly Guid MatchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public AwardMatchXpCommandHandlerTests()
    {
        _sut = new AwardMatchXpCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenPlayerHasNoXpYet_CreatesRecordWithAwardedXp()
    {
        _repository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((PlayerXp?)null);
        PlayerXp? added = null;
        _repository.Add(Arg.Do<PlayerXp>(p => added = p));

        await _sut.Handle(new AwardMatchXpCommand(MatchId, UserId, 150), CancellationToken.None);

        added.Should().NotBeNull();
        added!.UserId.Should().Be(UserId);
        added.MatchXp.Should().Be(150);
        _repository.Received(1).MarkMatchXpAwarded(Arg.Is<AwardedMatchXp>(a => a.MatchId == MatchId && a.UserId == UserId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPlayerExists_AddsToExistingXpAndUpdates()
    {
        var existing = PlayerXp.Create(UserId, 0, 0);
        existing.AddMatchXp(100);
        _repository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.Handle(new AwardMatchXpCommand(MatchId, UserId, 50), CancellationToken.None);

        existing.MatchXp.Should().Be(150);
        _repository.Received(1).Update(existing);
        _repository.DidNotReceive().Add(Arg.Any<PlayerXp>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMatchXpAlreadyAwarded_DoesNothing()
    {
        // Simulates a redelivered/retried MatchResultSubmitted event: the dedup guard already has this pair.
        _repository.HasAwardedMatchXpAsync(MatchId, UserId, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Handle(new AwardMatchXpCommand(MatchId, UserId, 150), CancellationToken.None);

        await _repository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Add(Arg.Any<PlayerXp>());
        _repository.DidNotReceive().Update(Arg.Any<PlayerXp>());
        _repository.DidNotReceive().MarkMatchXpAwarded(Arg.Any<AwardedMatchXp>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
