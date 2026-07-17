namespace Comeback.Profile.Application.Tests.Commands.UpdateProfile;

using Comeback.Profile.Application.Features.Profiles.Commands.UpdateProfile;
using FluentValidation.TestHelper;
using Xunit;

public sealed class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    private static UpdateProfileCommand EmptyCommand => new(
        Guid.NewGuid(), null, null, null, null, null, null, null);

    [Fact]
    public void Validate_WhenAllFieldsAreNull_HasNoErrors()
    {
        _validator.TestValidate(EmptyCommand).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenAllFieldsAreValid_HasNoErrors()
    {
        var command = EmptyCommand with
        {
            DisplayName = "Dime Petrovic",
            Bio = "Midfielder from Belgrade.",
            AvatarUrl = "https://cdn.comeback.app/avatars/dime.jpg",
            Position = "Midfielder",
            SkillLevel = "Advanced",
        };
        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDisplayNameExceedsMaxLength_HasDisplayNameError()
    {
        var command = EmptyCommand with { DisplayName = new string('a', 101) };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Validate_WhenDisplayNameIsExactlyMaxLength_HasNoError()
    {
        var command = EmptyCommand with { DisplayName = new string('a', 100) };
        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Validate_WhenBioExceedsMaxLength_HasBioError()
    {
        var command = EmptyCommand with { Bio = new string('x', 501) };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Bio);
    }

    [Fact]
    public void Validate_WhenBioIsExactlyMaxLength_HasNoError()
    {
        var command = EmptyCommand with { Bio = new string('x', 500) };
        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.Bio);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("just text")]
    [InlineData("relative/path/to/image.jpg")]
    [InlineData("../relative")]
    public void Validate_WhenAvatarUrlIsNotAbsoluteUrl_HasAvatarUrlError(string url)
    {
        var command = EmptyCommand with { AvatarUrl = url };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Theory]
    [InlineData("https://cdn.comeback.app/avatar.jpg")]
    [InlineData("http://localhost/avatar.png")]
    [InlineData("ftp://cdn.comeback.app/avatar.jpg")]
    public void Validate_WhenAvatarUrlIsValidAbsoluteUrl_HasNoError(string url)
    {
        var command = EmptyCommand with { AvatarUrl = url };
        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Theory]
    [InlineData("Goalkeeper")]
    [InlineData("goalkeeper")]
    [InlineData("GOALKEEPER")]
    [InlineData("Defender")]
    [InlineData("Midfielder")]
    [InlineData("Forward")]
    public void Validate_WhenPositionIsValid_HasNoError(string position)
    {
        var command = EmptyCommand with { Position = position };
        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.Position);
    }

    [Theory]
    [InlineData("Striker")]
    [InlineData("Wing")]
    [InlineData("CB")]
    [InlineData("invalid")]
    public void Validate_WhenPositionIsInvalid_HasPositionError(string position)
    {
        var command = EmptyCommand with { Position = position };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Position);
    }

    [Theory]
    [InlineData("Beginner")]
    [InlineData("beginner")]
    [InlineData("Intermediate")]
    [InlineData("Advanced")]
    [InlineData("Professional")]
    public void Validate_WhenSkillLevelIsValid_HasNoError(string skillLevel)
    {
        var command = EmptyCommand with { SkillLevel = skillLevel };
        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.SkillLevel);
    }

    [Theory]
    [InlineData("Expert")]
    [InlineData("Elite")]
    [InlineData("bad")]
    public void Validate_WhenSkillLevelIsInvalid_HasSkillLevelError(string skillLevel)
    {
        var command = EmptyCommand with { SkillLevel = skillLevel };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.SkillLevel);
    }
}
