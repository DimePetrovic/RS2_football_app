namespace Comeback.Auth.Application.Tests.Commands.RefreshToken;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.Features.Auth.Commands.RefreshToken;
using Comeback.Auth.Application.Tests.Helpers;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager = UserManagerFactory.Create();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly RefreshTokenCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly TokenPair NewTokens = new(
        "new-access-token", DateTime.UtcNow.AddMinutes(15),
        "new-refresh-token", DateTime.UtcNow.AddDays(7));

    private static readonly ApplicationUser ExistingUser = new()
    {
        Id = UserId,
        Email = "player@comeback.com",
        UserName = "comeback_player",
        Role = UserRole.Player,
    };

    public RefreshTokenCommandHandlerTests()
    {
        _sut = new RefreshTokenCommandHandler(
            _userManager, _refreshTokenRepository, _unitOfWork, _jwtProvider);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_ReturnsNewAuthResponse()
    {
        var existingToken = RefreshToken.Create(UserId, "old-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RefreshTokenCommand("old-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("old-token", Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _userManager.FindByIdAsync(UserId.ToString()).Returns(ExistingUser);
        _jwtProvider.Generate(ExistingUser).Returns(NewTokens);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be(NewTokens.AccessToken);
        result.RefreshToken.Should().Be(NewTokens.RefreshToken);
        result.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_RevokesOldToken()
    {
        var existingToken = RefreshToken.Create(UserId, "old-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RefreshTokenCommand("old-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("old-token", Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _userManager.FindByIdAsync(UserId.ToString()).Returns(ExistingUser);
        _jwtProvider.Generate(ExistingUser).Returns(NewTokens);

        await _sut.Handle(command, CancellationToken.None);

        existingToken.IsActive.Should().BeFalse();
        _refreshTokenRepository.Received(1).Update(existingToken);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_AddsNewRefreshToken()
    {
        var existingToken = RefreshToken.Create(UserId, "old-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RefreshTokenCommand("old-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("old-token", Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _userManager.FindByIdAsync(UserId.ToString()).Returns(ExistingUser);
        _jwtProvider.Generate(ExistingUser).Returns(NewTokens);

        await _sut.Handle(command, CancellationToken.None);

        _refreshTokenRepository.Received(1).Add(Arg.Is<RefreshToken>(rt =>
            rt.Token == NewTokens.RefreshToken && rt.UserId == UserId && rt.IsActive));
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_SavesChanges()
    {
        var existingToken = RefreshToken.Create(UserId, "old-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var command = new RefreshTokenCommand("old-token", "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("old-token", Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _userManager.FindByIdAsync(UserId.ToString()).Returns(ExistingUser);
        _jwtProvider.Generate(ExistingUser).Returns(NewTokens);

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsNotFoundException()
    {
        _refreshTokenRepository.GetActiveByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var command = new RefreshTokenCommand("invalid-token", "127.0.0.1");

        await _sut.Invoking(s => s.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        var existingToken = RefreshToken.Create(UserId, "valid-token", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        _refreshTokenRepository.GetActiveByTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(existingToken);
        _userManager.FindByIdAsync(UserId.ToString()).Returns((ApplicationUser?)null);

        var command = new RefreshTokenCommand("valid-token", "127.0.0.1");

        await _sut.Invoking(s => s.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
