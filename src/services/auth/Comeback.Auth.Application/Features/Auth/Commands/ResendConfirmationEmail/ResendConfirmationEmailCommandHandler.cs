namespace Comeback.Auth.Application.Features.Auth.Commands.ResendConfirmationEmail;

using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

internal sealed class ResendConfirmationEmailCommandHandler : IRequestHandler<ResendConfirmationEmailCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public ResendConfirmationEmailCommandHandler(
        UserManager<ApplicationUser> userManager,
        IIntegrationEventPublisher integrationEventPublisher)
    {
        _userManager = userManager;
        _integrationEventPublisher = integrationEventPublisher;
    }

    public async Task Handle(ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email)
            ?? throw new NotFoundException("No account found with that email address.", "auth.email_not_registered");

        if (user.AccountStatus != AccountStatus.PendingEmailVerification)
            throw new ConflictException("Account is already active.", "account.already_active");

        var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        await _integrationEventPublisher.PublishAsync(
            new EmailVerificationRequestedIntegrationEvent(user.Id, user.Email!, user.UserName!, encodedToken),
            cancellationToken);
    }
}
