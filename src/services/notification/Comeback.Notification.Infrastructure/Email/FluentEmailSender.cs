namespace Comeback.Notification.Infrastructure.Email;

using Comeback.Notification.Application.Common.Interfaces;
using FluentEmail.Core;

internal sealed class FluentEmailSender : IEmailSender
{
    private readonly IFluentEmailFactory _emailFactory;

    public FluentEmailSender(IFluentEmailFactory emailFactory)
    {
        _emailFactory = emailFactory;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        await _emailFactory
            .Create()
            .To(toEmail, toName)
            .Subject(subject)
            .Body(htmlBody, isHtml: true)
            .SendAsync();
    }
}
