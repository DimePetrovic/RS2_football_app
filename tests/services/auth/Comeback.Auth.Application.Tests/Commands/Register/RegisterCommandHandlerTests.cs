namespace Comeback.Auth.Application.Tests.Commands.Register;

using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Application.Features.Auth.Commands.Register;
using Comeback.Auth.Application.Tests.Helpers;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.Auth.Domain.Events;
using Comeback.BuildingBlocks.Domain.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public sealed class RegisterCommandHandlerTests
{
    private readonly UserManager<ApplicationUser> _userManager = UserManagerFactory.Create();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterCommandHandler _sut;

    private static readonly RegisterCommand ValidCommand = new(
        "player@comeback.com", "comeback_player", "Password123!", "Password123!", "127.0.0.1");

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_userManager, _publisher, _unitOfWork);

        _userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("email-confirm-token");
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_ReturnsRegistrationResponse()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        var result = await _sut.Handle(ValidCommand, CancellationToken.None);

        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_CreatesUserWithPendingStatus()
    {
        ApplicationUser? captured = null;
        _userManager.CreateAsync(Arg.Do<ApplicationUser>(u => captured = u), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        captured!.AccountStatus.Should().Be(AccountStatus.PendingEmailVerification);
        captured.Email.Should().Be(ValidCommand.Email);
        captured.UserName.Should().Be(ValidCommand.Username);
        captured.Role.Should().Be(UserRole.Player);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_GeneratesEmailConfirmationToken()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        await _userManager.Received(1).GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_PublishesUserRegisteredDomainEventWithToken()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        await _sut.Handle(ValidCommand, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<UserRegisteredDomainEvent>(e =>
                e.Email == ValidCommand.Email &&
                e.Username == ValidCommand.Username &&
                e.VerificationToken == "email-confirm-token"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(
                new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." }));

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyTaken_ThrowsConflictException()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(
                new IdentityError { Code = "DuplicateUserName", Description = "Username already taken." }));

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenIdentityValidationFails_ThrowsBusinessRuleException()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." }));

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_WhenCreateFails_DoesNotGenerateToken()
    {
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(
                new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." }));

        await _sut.Invoking(s => s.Handle(ValidCommand, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();

        await _userManager.DidNotReceive().GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>());
    }
}
