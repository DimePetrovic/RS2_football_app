namespace Comeback.Notification.Application.Features.Emails.SendVerificationEmail;

using System.Reflection;
using Comeback.Notification.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

internal sealed class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand>
{
    private readonly IEmailSender _emailSender;
    private readonly string _frontendBaseUrl;

    // User-facing content is loaded from template assets, not embedded as string literals in code.
    private static readonly Assembly TemplateAssembly = typeof(SendVerificationEmailCommandHandler).Assembly;
    private static readonly string HtmlTemplate = ReadResource("VerificationEmail.html");
    private static readonly string Subject = ReadResource("VerificationEmail.subject.txt").Trim();

    public SendVerificationEmailCommandHandler(IEmailSender emailSender, IConfiguration configuration)
    {
        _emailSender = emailSender;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
    }

    public async Task Handle(SendVerificationEmailCommand command, CancellationToken cancellationToken)
    {
        var confirmUrl = $"{_frontendBaseUrl}/complete-profile?userId={command.UserId}&token={Uri.EscapeDataString(command.VerificationToken)}";

        var htmlBody = HtmlTemplate
            .Replace("{{username}}", command.Username)
            .Replace("{{confirmUrl}}", confirmUrl);

        await _emailSender.SendAsync(
            command.Email,
            command.Username,
            Subject,
            htmlBody,
            cancellationToken);
    }

    private static string ReadResource(string name)
    {
        using var stream = TemplateAssembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded template '{name}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
