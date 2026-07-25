namespace Comeback.Notification.Application.Tests.Entities;

using Comeback.Notification.Application.Entities;
using FluentAssertions;
using Xunit;

public sealed class InAppNotificationTests
{
    [Fact]
    public void Constructor_LeavesTitleAndBodyEmpty_SoClientsRenderFromTypeAndPayload()
    {
        var recipient = Guid.NewGuid();

        var notification = new InAppNotification(recipient, "MatchInvitation", """{"matchId":"x"}""");

        notification.RecipientUserId.Should().Be(recipient);
        notification.Type.Should().Be("MatchInvitation");
        notification.Payload.Should().Be("""{"matchId":"x"}""");
        // Text is never rendered on the server — the client localizes from Type + Payload.
        notification.Title.Should().BeEmpty();
        notification.Body.Should().BeEmpty();
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsRead_SetsFlagAndTimestamp()
    {
        var notification = new InAppNotification(Guid.NewGuid(), "MatchCancelled");

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRead_CalledTwice_KeepsTheFirstTimestamp()
    {
        var notification = new InAppNotification(Guid.NewGuid(), "MatchCancelled");
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        notification.MarkAsRead();

        // Re-reading an already-read notification must not move the timestamp forward.
        notification.ReadAt.Should().Be(firstReadAt);
    }
}
