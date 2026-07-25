namespace Comeback.Notification.Application.Tests.Commands;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using Comeback.Notification.Application.Features.Notifications.Commands.MarkRead;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class MarkNotificationReadCommandHandlerTests
{
    private readonly IInAppNotificationRepository _repository = Substitute.For<IInAppNotificationRepository>();
    private readonly INotificationUnitOfWork _unitOfWork = Substitute.For<INotificationUnitOfWork>();
    private readonly MarkNotificationReadCommandHandler _sut;

    public MarkNotificationReadCommandHandlerTests()
        => _sut = new MarkNotificationReadCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_OwnNotification_MarksItReadAndSaves()
    {
        var userId = Guid.NewGuid();
        var notification = new InAppNotification(userId, "MatchInvitation");
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        await _sut.Handle(new MarkNotificationReadCommand(notification.Id, userId), CancellationToken.None);

        notification.IsRead.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownNotification_ThrowsNotFoundWithCode()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((InAppNotification?)null);

        var act = () => _sut.Handle(
            new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Code.Should().Be("notification.not_found");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SomeoneElsesNotification_ThrowsForbiddenAndLeavesItUnread()
    {
        var owner = Guid.NewGuid();
        var intruder = Guid.NewGuid();
        var notification = new InAppNotification(owner, "MatchInvitation");
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var act = () => _sut.Handle(
            new MarkNotificationReadCommand(notification.Id, intruder), CancellationToken.None);

        (await act.Should().ThrowAsync<ForbiddenException>())
            .Which.Code.Should().Be("notification.access_forbidden");
        // The owner's notification must stay untouched.
        notification.IsRead.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
