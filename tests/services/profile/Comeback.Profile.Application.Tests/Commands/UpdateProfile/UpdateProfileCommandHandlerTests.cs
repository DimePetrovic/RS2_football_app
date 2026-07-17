namespace Comeback.Profile.Application.Tests.Commands.UpdateProfile;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.Features.Profiles.Commands.UpdateProfile;
using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProfileCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    private static UserProfile MakeProfile() => UserProfile.Create(
        UserId, "comeback_player", "player@comeback.com",
        "Petar", "Petrović", new DateOnly(1995, 6, 15),
        Position.Midfielder, canPlayGoalkeeper: false,
        youthSeasons: 3, seniorSeasons: 5, "Player");

    public UpdateProfileCommandHandlerTests()
    {
        _sut = new UpdateProfileCommandHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsUpdatedResponse()
    {
        var command = new UpdateProfileCommand(
            UserId, "Dime", "Midfielder from Belgrade", null, "Midfielder", null, "Advanced", null);
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.DisplayName.Should().Be("Dime");
        result.Bio.Should().Be("Midfielder from Belgrade");
        result.PreferredPosition.Should().Be("Midfielder");
        result.SkillLevel.Should().Be("Advanced");
        result.UserId.Should().Be(UserId);
        result.Username.Should().Be("comeback_player");
    }

    [Fact]
    public async Task Handle_WhenProfileExists_CallsRepositoryUpdate()
    {
        var profile = MakeProfile();
        var command = new UpdateProfileCommand(UserId, "Dime", null, null, null, null, null, null);
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).Update(profile);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_SavesChanges()
    {
        var command = new UpdateProfileCommand(UserId, null, null, null, null, null, null, null);
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());

        await _sut.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var command = new UpdateProfileCommand(UserId, null, null, null, null, null, null, null);

        await _sut.Invoking(s => s.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_DoesNotSaveChanges()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var command = new UpdateProfileCommand(UserId, null, null, null, null, null, null, null);

        await _sut.Invoking(s => s.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Goalkeeper", Position.Goalkeeper)]
    [InlineData("goalkeeper", Position.Goalkeeper)]
    [InlineData("Defender", Position.Defender)]
    [InlineData("Midfielder", Position.Midfielder)]
    [InlineData("Forward", Position.Forward)]
    public async Task Handle_WhenPositionProvided_ParsesCorrectly(string positionString, Position expectedPosition)
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());
        var command = new UpdateProfileCommand(UserId, null, null, null, positionString, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.PreferredPosition.Should().Be(expectedPosition.ToString());
    }

    [Theory]
    [InlineData("Beginner", SkillLevel.Beginner)]
    [InlineData("intermediate", SkillLevel.Intermediate)]
    [InlineData("Advanced", SkillLevel.Advanced)]
    [InlineData("Professional", SkillLevel.Professional)]
    public async Task Handle_WhenSkillLevelProvided_ParsesCorrectly(string skillString, SkillLevel expectedSkill)
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());
        var command = new UpdateProfileCommand(UserId, null, null, null, null, null, skillString, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.SkillLevel.Should().Be(expectedSkill.ToString());
    }

    [Fact]
    public async Task Handle_WhenPositionIsGoalkeeper_ForcesCanPlayGoalkeeper()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());
        var command = new UpdateProfileCommand(UserId, null, null, null, "Goalkeeper", false, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.CanPlayGoalkeeper.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOptionalFieldsNull_ClearsThemButKeepsPosition()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());
        var command = new UpdateProfileCommand(UserId, null, null, null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.DisplayName.Should().BeNull();
        result.Bio.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
        result.SkillLevel.Should().BeNull();
        // The position is not cleared — without a new value it keeps the one from profile creation
        result.PreferredPosition.Should().Be(Position.Midfielder.ToString());
    }

    [Fact]
    public async Task Handle_WhenProfileUpdated_UpdatesUpdatedAtTimestamp()
    {
        var profile = MakeProfile();
        var createdAt = profile.CreatedAt;
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = new UpdateProfileCommand(UserId, "New Name", null, null, null, null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.UpdatedAt.Should().BeOnOrAfter(createdAt);
    }
}
