namespace Comeback.Auth.Application.Tests.Commands.CompleteRegistration;

using System.Text;
using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.Features.Auth.Commands.CompleteRegistration;
using Comeback.Auth.Application.Tests.Helpers;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.IntegrationEvents.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using NSubstitute;
using Xunit;

public sealed class CompleteRegistrationCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager = UserManagerFactory.Create();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly IIntegrationEventPublisher _integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
    private readonly CompleteRegistrationCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private const string RawToken = "raw-email-confirm-token";
    private static readonly string EncodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(RawToken));

    private static readonly TokenPair SampleTokens = new(
        "access-token", DateTime.UtcNow.AddMinutes(15),
        "refresh-token", DateTime.UtcNow.AddDays(7));

    private static readonly CompleteRegistrationCommand ValidCommand = new(
        UserId.ToString(), EncodedToken,
        "Petar", "Petrović", new DateOnly(1995, 6, 15),
        PreferredPosition: 2, CanPlayGoalkeeper: false,
        YouthSeasons: 3, SeniorSeasons: 5,
        Nationality: null,
        "127.0.0.1");

    private static ApplicationUser MakePendingUser() => new()
    {
        Id = UserId,
        Email = "player@comeback.com",
        UserName = "comeback_player",
        Role = UserRole.Player,
        AccountStatus = AccountStatus.PendingEmailVerification,
    };

    public CompleteRegistrationCommandHandlerTests()
    {
        _sut = new CompleteRegistrationCommandHandler(
            _userManager, _refreshTokenRepository, _unitOfWork, _jwtProvider, _integrationEventPublisher);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_ReturnsAuthResponse()
    {
        var user = MakePendingUser();
        _userManager.FindByIdAsync(UserId.ToString()).Returns(user);
        _userManager.ConfirmEmailAsync(user, RawToken).Returns(IdentityResult.Success);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _jwtProvider.Generate(user).Returns(SampleTokens);

        var result = await _sut.Handle(ValidCommand, CancellationToken.None);

        result.AccessToken.Should().Be(SampleTokens.AccessToken);
        result.UserId.Should().Be(UserId);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_SetsAccountStatusToActive()
    {
        var user = MakePendingUser();
        _userManager.FindByIdAsync(UserId.ToString()).Returns(user);
        _userManager.ConfirmEmailAsync(user, RawToken).Returns(IdentityResult.Success);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _jwtProvider.Generate(user).Returns(SampleTokens);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        user.AccountStatus.Should().Be(AccountStatus.Active);
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_PublishesUserEmailConfirmedIntegrationEventWithProfileData()
    {
        var user = MakePendingUser();
        _userManager.FindByIdAsync(UserId.ToString()).Returns(user);
        _userManager.ConfirmEmailAsync(user, RawToken).Returns(IdentityResult.Success);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _jwtProvider.Generate(user).Returns(SampleTokens);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        await _integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<UserEmailConfirmedIntegrationEvent>(e =>
                e.UserId == UserId &&
                e.Email == user.Email &&
                e.Username == user.UserName &&
                e.FirstName == ValidCommand.FirstName &&
                e.LastName == ValidCommand.LastName &&
                e.DateOfBirth == ValidCommand.DateOfBirth &&
                e.PreferredPosition == ValidCommand.PreferredPosition &&
                e.CanPlayGoalkeeper == ValidCommand.CanPlayGoalkeeper &&
                e.YouthSeasons == ValidCommand.YouthSeasons &&
                e.SeniorSeasons == ValidCommand.SeniorSeasons),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_AddsRefreshTokenAndSavesChanges()
    {
        var user = MakePendingUser();
        _userManager.FindByIdAsync(UserId.ToString()).Returns(user);
        _userManager.ConfirmEmailAsync(user, RawToken).Returns(IdentityResult.Success);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _jwtProvider.Generate(user).Returns(SampleTokens);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        _refreshTokenRepository.Received(1).Add(Arg.Is<RefreshToken>(rt =>
            rt.Token == SampleTokens.RefreshToken && rt.UserId == UserId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userManager.FindByIdAsync(UserId.ToString()).Returns((ApplicationUser?)null);

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ThrowsConflictException()
    {
        var activeUser = new ApplicationUser
        {
            Id = UserId,
            Email = "player@comeback.com",
            UserName = "comeback_player",
            AccountStatus = AccountStatus.Active,
        };
        _userManager.FindByIdAsync(UserId.ToString()).Returns(activeUser);

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenTokenIsInvalid_ThrowsBusinessRuleException()
    {
        var user = MakePendingUser();
        _userManager.FindByIdAsync(UserId.ToString()).Returns(user);
        _userManager.ConfirmEmailAsync(user, RawToken)
            .Returns(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." }));

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
