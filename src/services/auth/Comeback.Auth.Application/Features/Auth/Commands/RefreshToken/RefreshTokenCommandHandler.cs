namespace Comeback.Auth.Application.Features.Auth.Commands.RefreshToken;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.DTOs;
using Comeback.Auth.Domain.Entities;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;

    public RefreshTokenCommandHandler(
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

    public async Task<AuthResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository.GetActiveByTokenAsync(command.Token, cancellationToken)
            ?? throw new NotFoundException("Refresh token is invalid or expired.", "auth.refresh_token_invalid");

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString())
            ?? throw new NotFoundException("User not found.", "user.not_found");

        existingToken.Revoke(command.IpAddress);
        _refreshTokenRepository.Update(existingToken);

        var tokens = _jwtProvider.Generate(user);
        var newRefreshToken = RefreshToken.Create(user.Id, tokens.RefreshToken, tokens.RefreshTokenExpiresAt, command.IpAddress);

        _refreshTokenRepository.Add(newRefreshToken);
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
