namespace Comeback.Notification.Application.Features.Emails.SendVerificationEmail;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record SendVerificationEmailCommand(
    Guid UserId,
    string Email,
    string Username,
    string VerificationToken) : ICommand;
