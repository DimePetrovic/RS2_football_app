namespace Comeback.Notification.Application.Tests.Queries;

using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using Comeback.Notification.Application.Features.Notifications.Queries.GetNotifications;
using Comeback.Notification.Application.Features.Notifications.Queries.GetUnreadCount;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class GetNotificationsQueryHandlerTests
{
    private readonly IInAppNotificationRepository _repository = Substitute.For<IInAppNotificationRepository>();
    private readonly GetNotificationsQueryHandler _sut;

    public GetNotificationsQueryHandlerTests()
        => _sut = new GetNotificationsQueryHandler(_repository);

    [Fact]
    public async Task Handle_CurrentRows_ExposeTypeAndPayloadWithoutLegacyText()
    {
        var userId = Guid.NewGuid();
        var notification = new InAppNotification(userId, "MatchCancelled", """{"matchId":"abc"}""");
        _repository.GetByRecipientAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<InAppNotification> { notification });

        var result = await _sut.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        var response = result.Should().ContainSingle().Subject;
        response.Type.Should().Be("MatchCancelled");
        response.Payload.Should().Be("""{"matchId":"abc"}""");
        // Empty Title/Body must surface as null so the client knows to localize from Type + Payload
        // instead of rendering blank strings.
        response.LegacyTitle.Should().BeNull();
        response.LegacyBody.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RowsFromBeforeTheRearchitecture_StillSurfaceTheirStoredText()
    {
        var userId = Guid.NewGuid();
        var legacy = new InAppNotification(userId, "MatchInvitation");
        SetPrivate(legacy, nameof(InAppNotification.Title), "Poziv na meč");
        SetPrivate(legacy, nameof(InAppNotification.Body), "Pozvani ste na meč.");
        _repository.GetByRecipientAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<InAppNotification> { legacy });

        var result = await _sut.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        var response = result.Should().ContainSingle().Subject;
        response.LegacyTitle.Should().Be("Poziv na meč");
        response.LegacyBody.Should().Be("Pozvani ste na meč.");
    }

    [Fact]
    public async Task Handle_ForwardsTheRequestedLimit()
    {
        var userId = Guid.NewGuid();
        _repository.GetByRecipientAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<InAppNotification>());

        await _sut.Handle(new GetNotificationsQuery(userId, Limit: 10), CancellationToken.None);

        await _repository.Received(1).GetByRecipientAsync(userId, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsTheCountForTheCaller()
    {
        var userId = Guid.NewGuid();
        _repository.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>()).Returns(4);
        var sut = new GetUnreadCountQueryHandler(_repository);

        var count = await sut.Handle(new GetUnreadCountQuery(userId), CancellationToken.None);

        count.Should().Be(4);
    }

    /// <summary>
    /// Writes a private-setter property the way EF does when it materializes an old row.
    /// Those rows cannot be produced through the constructor, which always blanks Title/Body.
    /// </summary>
    private static void SetPrivate(InAppNotification notification, string property, object? value)
        => typeof(InAppNotification).GetProperty(property)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(notification, new[] { value });
}
