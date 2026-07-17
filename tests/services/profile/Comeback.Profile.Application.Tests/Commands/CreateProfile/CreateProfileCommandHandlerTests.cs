namespace Comeback.Profile.Application.Tests.Commands.CreateProfile;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.IntegrationEvents.Profile;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.Features.Profiles.Commands.CreateProfile;
using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class CreateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventPublisher _publisher = Substitute.For<IIntegrationEventPublisher>();
    private readonly CreateProfileCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly CreateProfileCommand ValidCommand = new(
        UserId, "comeback_player", "player@comeback.com",
        "Petar", "Petrović", new DateOnly(1995, 6, 15),
        PreferredPosition: (int)Position.Midfielder, CanPlayGoalkeeper: false,
        YouthSeasons: 3, SeniorSeasons: 5, "Player", Nationality: null);

    public CreateProfileCommandHandlerTests()
    {
        _sut = new CreateProfileCommandHandler(_repository, _unitOfWork, _publisher);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_AddsProfileToRepository()
    {
        await _sut.Handle(ValidCommand, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<UserProfile>(p =>
            p.UserId == UserId &&
            p.Username == ValidCommand.Username &&
            p.Email == ValidCommand.Email));
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_SavesChanges()
    {
        await _sut.Handle(ValidCommand, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_MapsCareerFields()
    {
        UserProfile? captured = null;
        _repository.Add(Arg.Do<UserProfile>(p => captured = p));

        await _sut.Handle(ValidCommand, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.FirstName.Should().Be("Petar");
        captured.LastName.Should().Be("Petrović");
        captured.DateOfBirth.Should().Be(new DateOnly(1995, 6, 15));
        captured.PreferredPosition.Should().Be(Position.Midfielder);
        captured.CanPlayGoalkeeper.Should().BeFalse();
        captured.YouthSeasons.Should().Be(3);
        captured.SeniorSeasons.Should().Be(5);
        captured.Role.Should().Be("Player");
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_SetsDefaultDisplayNameToFullName()
    {
        UserProfile? captured = null;
        _repository.Add(Arg.Do<UserProfile>(p => captured = p));

        await _sut.Handle(ValidCommand, CancellationToken.None);

        captured!.DisplayName.Should().Be("Petar Petrović");
    }

    [Fact]
    public async Task Handle_WhenPositionIsGoalkeeper_ForcesCanPlayGoalkeeper()
    {
        UserProfile? captured = null;
        _repository.Add(Arg.Do<UserProfile>(p => captured = p));
        var command = ValidCommand with
        {
            PreferredPosition = (int)Position.Goalkeeper,
            CanPlayGoalkeeper = false,
        };

        await _sut.Handle(command, CancellationToken.None);

        captured!.CanPlayGoalkeeper.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_PublishesPlayerCareerDataUpdatedIntegrationEvent()
    {
        await _sut.Handle(ValidCommand, CancellationToken.None);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<PlayerCareerDataUpdatedIntegrationEvent>(e =>
                e.UserId == UserId &&
                e.YouthSeasons == ValidCommand.YouthSeasons &&
                e.SeniorSeasons == ValidCommand.SeniorSeasons),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_OptionalFieldsStartEmpty()
    {
        UserProfile? captured = null;
        _repository.Add(Arg.Do<UserProfile>(p => captured = p));

        await _sut.Handle(ValidCommand, CancellationToken.None);

        captured!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        captured.Bio.Should().BeNull();
        captured.AvatarUrl.Should().BeNull();
        captured.SkillLevel.Should().BeNull();
    }
}
