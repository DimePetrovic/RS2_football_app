namespace Comeback.Auth.Application.Features.Auth.Events;

using System.Text;
using Comeback.Auth.Domain.Events;
using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.IntegrationEvents.Auth;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;

internal sealed class UserRegisteredDomainEventHandler : INotificationHandler<UserRegisteredDomainEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public UserRegisteredDomainEventHandler(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(UserRegisteredDomainEvent notification, CancellationToken cancellationToken)
    {
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(notification.VerificationToken));

        await _publisher.PublishAsync(
            new EmailVerificationRequestedIntegrationEvent(
                notification.UserId,
                notification.Email,
                notification.Username,
                encodedToken),
            cancellationToken);
    }
}
