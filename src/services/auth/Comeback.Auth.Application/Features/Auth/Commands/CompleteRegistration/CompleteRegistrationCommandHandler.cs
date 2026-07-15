namespace Comeback.Auth.Application.Features.Auth.Commands.CompleteRegistration;

using System.Text;
using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.DTOs;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

internal sealed class CompleteRegistrationCommandHandler : IRequestHandler<CompleteRegistrationCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public CompleteRegistrationCommandHandler(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IJwtProvider jwtProvider,
        IIntegrationEventPublisher integrationEventPublisher)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
        _integrationEventPublisher = integrationEventPublisher;
    }

    public async Task<AuthResponse> Handle(CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId)
            ?? throw new NotFoundException("User not found.", "user.not_found");

        var nationality = NormalizeNationality(command.Nationality);

        if (user.AccountStatus == AccountStatus.Active)
            throw new ConflictException("Account is already active.", "account.already_active");

        var rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(command.Token));
        var result = await _userManager.ConfirmEmailAsync(user, rawToken);

        if (!result.Succeeded)
            throw new BusinessRuleException("Invalid or expired email confirmation token.", "auth.invalid_confirmation_token");

        user.AccountStatus = AccountStatus.Active;
        await _userManager.UpdateAsync(user);

        await _integrationEventPublisher.PublishAsync(
            new UserEmailConfirmedIntegrationEvent(
                user.Id,
                user.Email!,
                user.UserName!,
                command.FirstName,
                command.LastName,
                command.DateOfBirth,
                command.PreferredPosition,
                command.CanPlayGoalkeeper,
                command.YouthSeasons,
                command.SeniorSeasons,
                user.Role.ToString(),
                nationality),
            cancellationToken);

        var tokens = _jwtProvider.Generate(user);
        var refreshToken = RefreshToken.Create(user.Id, tokens.RefreshToken, tokens.RefreshTokenExpiresAt, command.IpAddress);

        _refreshTokenRepository.Add(refreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            user.Id,
            user.UserName!,
            user.Email!,
            user.Role.ToString());
    }

    /// <summary>ISO 3166-1 alpha-2 or null. "XK" is not an officially assigned ISO code and is rejected.</summary>
    private static string? NormalizeNationality(string? nationality)
    {
        if (string.IsNullOrWhiteSpace(nationality)) return null;
        var code = nationality.Trim().ToUpperInvariant();
        if (code.Length != 2 || !code.All(char.IsAsciiLetterUpper) || code == "XK")
            throw new BusinessRuleException("Invalid nationality code.", "profile.invalid_nationality");
        return code;
    }
}
