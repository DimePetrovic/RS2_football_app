namespace Comeback.Auth.Application.Features.Auth.Commands.Login;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.DTOs;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IJwtProvider jwtProvider)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
    }

    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email)
            ?? throw new NotFoundException("Invalid email or password.", "auth.invalid_credentials");

        if (!await _userManager.CheckPasswordAsync(user, command.Password))
            throw new NotFoundException("Invalid email or password.", "auth.invalid_credentials");

        if (user.AccountStatus == AccountStatus.PendingEmailVerification)
            throw new ForbiddenException("Please confirm your email address before logging in.", "auth.email_not_confirmed");

        if (user.AccountStatus == AccountStatus.Suspended)
            throw new ForbiddenException("Your account has been suspended.", "auth.account_suspended");

        if (user.AccountStatus == AccountStatus.Deactivated)
            throw new ForbiddenException("Your account has been deactivated.", "auth.account_deactivated");

        await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(user.Id, command.IpAddress, cancellationToken);

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
}
