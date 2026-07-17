namespace Comeback.Profile.Application.Tests.Queries.GetProfileByUserId;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Application.Features.Profiles.Queries.GetProfileByUserId;
using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class GetProfileByUserIdQueryHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly GetProfileByUserIdQueryHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    private static UserProfile MakeProfile() => UserProfile.Create(
        UserId, "comeback_player", "player@comeback.com",
        "Petar", "Petrović", new DateOnly(1995, 6, 15),
        Position.Midfielder, canPlayGoalkeeper: false,
        youthSeasons: 3, seniorSeasons: 5, "Player");

    public GetProfileByUserIdQueryHandlerTests()
    {
        _sut = new GetProfileByUserIdQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsProfileResponse()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());

        var result = await _sut.Handle(new GetProfileByUserIdQuery(UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(UserId);
        result.Username.Should().Be("comeback_player");
        result.Email.Should().Be("player@comeback.com");
    }

    [Fact]
    public async Task Handle_WhenProfileExists_MapsAllFields()
    {
        var profile = MakeProfile();
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _sut.Handle(new GetProfileByUserIdQuery(UserId), CancellationToken.None);

        result.Id.Should().Be(profile.Id);
        result.UserId.Should().Be(profile.UserId);
        result.Username.Should().Be(profile.Username);
        result.Email.Should().Be(profile.Email);
        result.FirstName.Should().Be(profile.FirstName);
        result.LastName.Should().Be(profile.LastName);
        result.DateOfBirth.Should().Be(profile.DateOfBirth);
        result.PreferredPosition.Should().Be(profile.PreferredPosition.ToString());
        result.CanPlayGoalkeeper.Should().Be(profile.CanPlayGoalkeeper);
        result.YouthSeasons.Should().Be(profile.YouthSeasons);
        result.SeniorSeasons.Should().Be(profile.SeniorSeasons);
        result.DisplayName.Should().Be(profile.DisplayName);
        result.Bio.Should().Be(profile.Bio);
        result.AvatarUrl.Should().Be(profile.AvatarUrl);
        result.SkillLevel.Should().Be(profile.SkillLevel?.ToString());
        result.CreatedAt.Should().Be(profile.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        await _sut.Invoking(s => s.Handle(new GetProfileByUserIdQuery(UserId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenProfileUpdated_ReturnsMappedStrings()
    {
        var profile = MakeProfile();
        profile.Update("Dime", null, null, Position.Forward, null, SkillLevel.Advanced, nationality: null);
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _sut.Handle(new GetProfileByUserIdQuery(UserId), CancellationToken.None);

        result.PreferredPosition.Should().Be("Forward");
        result.SkillLevel.Should().Be("Advanced");
    }

    [Fact]
    public async Task Handle_WhenProfileHasNoOptionalFields_ReturnsNulls()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());

        var result = await _sut.Handle(new GetProfileByUserIdQuery(UserId), CancellationToken.None);

        result.Bio.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
        result.SkillLevel.Should().BeNull();
        // DisplayName is set automatically to "First Last" on profile creation
        result.DisplayName.Should().Be("Petar Petrović");
    }

    [Fact]
    public async Task Handle_WhenCalled_QueriesRepositoryByUserId()
    {
        _repository.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(MakeProfile());

        await _sut.Handle(new GetProfileByUserIdQuery(UserId), CancellationToken.None);

        await _repository.Received(1).GetByIdAsync(UserId, Arg.Any<CancellationToken>());
    }
}
