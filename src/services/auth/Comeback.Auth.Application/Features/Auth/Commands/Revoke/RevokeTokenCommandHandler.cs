namespace Comeback.Auth.Application.Features.Auth.Commands.Revoke;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;

internal sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetActiveByTokenAsync(command.Token, cancellationToken)
            ?? throw new NotFoundException("Refresh token is invalid or expired.", "auth.refresh_token_invalid");

        refreshToken.Revoke(command.IpAddress);
        _refreshTokenRepository.Update(refreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
