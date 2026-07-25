namespace Comeback.Notification.Application.Tests.Commands;

using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Features.Notifications.Commands.MarkAllRead;
using NSubstitute;
using Xunit;

public sealed class MarkAllNotificationsReadCommandHandlerTests
{
    private readonly IInAppNotificationRepository _repository = Substitute.For<IInAppNotificationRepository>();
    private readonly INotificationUnitOfWork _unitOfWork = Substitute.For<INotificationUnitOfWork>();
    private readonly MarkAllNotificationsReadCommandHandler _sut;

    public MarkAllNotificationsReadCommandHandlerTests()
        => _sut = new MarkAllNotificationsReadCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_MarksOnlyTheCallersNotificationsAndCommits()
    {
        var userId = Guid.NewGuid();

        await _sut.Handle(new MarkAllNotificationsReadCommand(userId), CancellationToken.None);

        // The user id must be forwarded — a bulk update scoped to the wrong user would clear
        // somebody else's notifications.
        await _repository.Received(1).MarkAllReadAsync(userId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
