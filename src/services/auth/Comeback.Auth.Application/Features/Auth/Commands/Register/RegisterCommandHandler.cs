namespace Comeback.Auth.Application.Features.Auth.Commands.Register;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.DTOs;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.Auth.Domain.Events;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegistrationResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IPublisher publisher,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegistrationResponse> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            UserName = command.Username,
            Role = UserRole.Player,
            AccountStatus = AccountStatus.PendingEmailVerification,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            // Surface the Identity error code so the client can localize it.
            var code = $"auth.identity.{error.Code}";
            throw error.Code is "DuplicateEmail" or "DuplicateUserName"
                ? new ConflictException(error.Description, code)
                : new BusinessRuleException(error.Description, code);
        }

        var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        await _publisher.Publish(
            new UserRegisteredDomainEvent(user.Id, user.Email!, user.UserName!, verificationToken),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegistrationResponse(
            "Registration successful. Please check your email to activate your account.");
    }
}
