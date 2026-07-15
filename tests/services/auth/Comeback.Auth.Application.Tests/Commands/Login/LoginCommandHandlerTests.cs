namespace Comeback.Auth.Application.Tests.Commands.Login;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.Features.Auth.Commands.Login;
using Comeback.Auth.Application.Tests.Helpers;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public sealed class LoginCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager = UserManagerFactory.Create();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly LoginCommandHandler _sut;

    private static readonly LoginCommand ValidCommand = new(
        "player@comeback.com", "Password123!", "127.0.0.1");

    private static readonly TokenPair SampleTokens = new(
        "access-token", DateTime.UtcNow.AddMinutes(15),
        "refresh-token", DateTime.UtcNow.AddDays(7));

    private static readonly ApplicationUser ExistingUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "player@comeback.com",
        UserName = "comeback_player",
        Role = UserRole.Player,
        AccountStatus = AccountStatus.Active,
    };

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(
            _userManager, _refreshTokenRepository, _unitOfWork, _jwtProvider);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsAuthResponse()
    {
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns(ExistingUser);
        _userManager.CheckPasswordAsync(ExistingUser, ValidCommand.Password).Returns(true);
        _jwtProvider.Generate(ExistingUser).Returns(SampleTokens);

        var result = await _sut.Handle(ValidCommand, CancellationToken.None);

        result.AccessToken.Should().Be(SampleTokens.AccessToken);
        result.UserId.Should().Be(ExistingUser.Id);
        result.Email.Should().Be(ExistingUser.Email);
        result.Username.Should().Be(ExistingUser.UserName);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_AddsRefreshToken()
    {
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns(ExistingUser);
        _userManager.CheckPasswordAsync(ExistingUser, ValidCommand.Password).Returns(true);
        _jwtProvider.Generate(ExistingUser).Returns(SampleTokens);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        _refreshTokenRepository.Received(1).Add(Arg.Is<RefreshToken>(rt =>
            rt.Token == SampleTokens.RefreshToken && rt.UserId == ExistingUser.Id));
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_SavesChanges()
    {
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns(ExistingUser);
        _userManager.CheckPasswordAsync(ExistingUser, ValidCommand.Password).Returns(true);
        _jwtProvider.Generate(ExistingUser).Returns(SampleTokens);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns((ApplicationUser?)null);

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ThrowsNotFoundException()
    {
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns(ExistingUser);
        _userManager.CheckPasswordAsync(ExistingUser, ValidCommand.Password).Returns(false);

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Invalid email or password*");
    }

    [Theory]
    [InlineData(AccountStatus.PendingEmailVerification)]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Deactivated)]
    public async Task Handle_WhenAccountIsNotActive_ThrowsForbiddenException(AccountStatus status)
    {
        var inactiveUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = ValidCommand.Email,
            UserName = "comeback_player",
            Role = UserRole.Player,
            AccountStatus = status,
        };
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns(inactiveUser);
        _userManager.CheckPasswordAsync(inactiveUser, ValidCommand.Password).Returns(true);

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_DoesNotAddRefreshToken()
    {
        _userManager.FindByEmailAsync(ValidCommand.Email).Returns(ExistingUser);
        _userManager.CheckPasswordAsync(ExistingUser, ValidCommand.Password).Returns(false);

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        _refreshTokenRepository.DidNotReceive().Add(Arg.Any<RefreshToken>());
    }
}
