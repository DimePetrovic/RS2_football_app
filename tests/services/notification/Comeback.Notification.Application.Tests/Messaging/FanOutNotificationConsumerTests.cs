namespace Comeback.Notification.Application.Tests.Messaging;

using System.Text.Json;
using Comeback.BuildingBlocks.IntegrationEvents.Match;
using Comeback.Notification.Application.Common.Interfaces;
using Comeback.Notification.Application.Entities;
using Comeback.Notification.Infrastructure.Messaging;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using Xunit;

/// <summary>
/// Covers the fan-out that every in-app notification consumer inherits: resolve recipients →
/// persist one notification each → save → push.
/// </summary>
public sealed class FanOutNotificationConsumerTests
{
    private readonly IInAppNotificationRepository _repository = Substitute.For<IInAppNotificationRepository>();
    private readonly INotificationUnitOfWork _unitOfWork = Substitute.For<INotificationUnitOfWork>();
    private readonly INotificationPusher _pusher = Substitute.For<INotificationPusher>();

    [Fact]
    public async Task Consume_NoRecipients_TouchesNeitherDatabaseNorClients()
    {
        var sut = new TestConsumer(_repository, _unitOfWork, _pusher, Array.Empty<Guid>());

        await sut.Consume(ContextFor(new TestEvent("ignored")));

        _repository.DidNotReceive().Add(Arg.Any<InAppNotification>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pusher.DidNotReceive().PushAsync(Arg.Any<InAppNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_PersistsOneNotificationPerRecipientAndPushesEach()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var saved = new List<InAppNotification>();
        _repository.Add(Arg.Do<InAppNotification>(saved.Add));
        var sut = new TestConsumer(_repository, _unitOfWork, _pusher, new[] { first, second });

        await sut.Consume(ContextFor(new TestEvent("payload-value")));

        saved.Should().HaveCount(2);
        saved.Select(n => n.RecipientUserId).Should().BeEquivalentTo(new[] { first, second });
        saved.Should().OnlyContain(n => n.Type == "TestType");
        saved.Should().OnlyContain(n => n.Payload == """{"value":"payload-value"}""");
        // One transaction for the whole fan-out, one push per recipient.
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pusher.Received(2).PushAsync(Arg.Any<InAppNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_SavesBeforePushing_SoNothingIsDeliveredThatIsNotPersisted()
    {
        var sut = new TestConsumer(_repository, _unitOfWork, _pusher, new[] { Guid.NewGuid() });

        await sut.Consume(ContextFor(new TestEvent("x")));

        // If the push went first, a failed save would leave the client showing a notification
        // that does not exist in the database.
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _pusher.PushAsync(Arg.Any<InAppNotification>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Consume_MatchInvitation_StoresTheTypeAndPayloadTheClientLocalizesFrom()
    {
        InAppNotification? saved = null;
        _repository.Add(Arg.Do<InAppNotification>(n => saved = n));
        var invitee = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var startsAt = new DateTime(2026, 8, 1, 18, 30, 0, DateTimeKind.Utc);
        var sut = new MatchInvitationSentConsumer(_repository, _unitOfWork, _pusher);

        await sut.Consume(ContextFor(new MatchInvitationSentIntegrationEvent(
            matchId, "Petak 18h", Guid.NewGuid(), "Marko", invitee, startsAt, "Hala 1")));

        saved.Should().NotBeNull();
        saved!.RecipientUserId.Should().Be(invitee);
        saved.Type.Should().Be("MatchInvitation");
        var payload = JsonDocument.Parse(saved.Payload!).RootElement;
        payload.GetProperty("matchId").GetGuid().Should().Be(matchId);
        payload.GetProperty("organizerName").GetString().Should().Be("Marko");
        payload.GetProperty("location").GetString().Should().Be("Hala 1");
        // No server-rendered sentence anywhere — only machine-readable fields.
        saved.Title.Should().BeEmpty();
        saved.Body.Should().BeEmpty();
    }

    private static ConsumeContext<T> ContextFor<T>(T message) where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }

    /// <summary>Public so NSubstitute can proxy <c>ConsumeContext&lt;TestEvent&gt;</c>.</summary>
    public sealed record TestEvent(string Value);

    private sealed class TestConsumer : FanOutNotificationConsumer<TestEvent>
    {
        private readonly IReadOnlyCollection<Guid> _recipients;

        public TestConsumer(
            IInAppNotificationRepository repository,
            INotificationUnitOfWork unitOfWork,
            INotificationPusher pusher,
            IReadOnlyCollection<Guid> recipients) : base(repository, unitOfWork, pusher)
            => _recipients = recipients;

        protected override string GetNotificationType(TestEvent e) => "TestType";

        protected override object BuildPayload(TestEvent e) => new { value = e.Value };

        protected override Task<IReadOnlyCollection<Guid>> GetRecipientsAsync(TestEvent e, CancellationToken ct)
            => Task.FromResult(_recipients);
    }
}
