namespace Comeback.Auth.Application.Tests.Commands.Revoke;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.Features.Auth.Commands.Revoke;
using Comeback.Auth.Domain.Entities;
using Comeback.BuildingBlocks.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class RevokeTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RevokeTokenCommandHandler _sut;

    public RevokeTokenCommandHandlerTests()
    {
        _sut = new RevokeTokenCommandHandler(_refreshTokenRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_RevokesToken()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RevokeTokenCommand("valid-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(token);

        await _sut.Handle(command, CancellationToken.None);

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_UpdatesRepository()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RevokeTokenCommand("valid-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(token);

        await _sut.Handle(command, CancellationToken.None);

        _refreshTokenRepository.Received(1).Update(token);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_SavesChanges()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RevokeTokenCommand("valid-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(token);

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsNotFoundException()
    {
        _refreshTokenRepository.GetActiveByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var command = new RevokeTokenCommand("invalid-token", "127.0.0.1");

        await _sut.Invoking(s => s.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_DoesNotSaveChanges()
    {
        _refreshTokenRepository.GetActiveByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var command = new RevokeTokenCommand("invalid-token", "127.0.0.1");

        await _sut.Invoking(s => s.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
