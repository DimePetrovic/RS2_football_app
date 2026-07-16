namespace Comeback.Notification.Infrastructure.Settings;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string FromEmail { get; init; } = "noreply@comeback.app";
    public string FromName { get; init; } = "Comeback";
}
