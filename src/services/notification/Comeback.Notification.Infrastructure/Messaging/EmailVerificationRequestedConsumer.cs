namespace Comeback.Notification.Infrastructure.Messaging;

using Comeback.BuildingBlocks.IntegrationEvents.Auth;
using Comeback.Notification.Application.Features.Emails.SendVerificationEmail;
using MassTransit;
using MediatR;

internal sealed class EmailVerificationRequestedConsumer : IConsumer<EmailVerificationRequestedIntegrationEvent>
{
    private readonly ISender _sender;

    public EmailVerificationRequestedConsumer(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<EmailVerificationRequestedIntegrationEvent> context)
    {
        var message = context.Message;
        await _sender.Send(
            new SendVerificationEmailCommand(
                message.UserId,
                message.Email,
                message.Username,
                message.VerificationToken),
            context.CancellationToken);
    }
}
