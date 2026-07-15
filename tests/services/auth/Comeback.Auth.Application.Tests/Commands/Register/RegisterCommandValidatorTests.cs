namespace Comeback.Auth.Application.Tests.Commands.Register;

using Comeback.Auth.Application.Features.Auth.Commands.Register;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand => new(
        "player@comeback.com", "comeback_player", "Password123!", "Password123!", "127.0.0.1");

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain")]
    public void Validate_WhenEmailIsInvalid_HasEmailError(string email)
    {
        var command = ValidCommand with { Email = email };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("user@name")]
    [InlineData("")]
    public void Validate_WhenUsernameIsInvalid_HasUsernameError(string username)
    {
        var command = ValidCommand with { Username = username };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WhenUsernameExceedsMaxLength_HasUsernameError()
    {
        var command = ValidCommand with { Username = new string('a', 31) };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WhenPasswordIsTooShort_HasPasswordError()
    {
        var command = ValidCommand with { Password = "Short1!", ConfirmPassword = "Short1!" };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenPasswordIsEmpty_HasPasswordError()
    {
        var command = ValidCommand with { Password = "", ConfirmPassword = "" };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenPasswordsDoNotMatch_HasConfirmPasswordError()
    {
        var command = ValidCommand with { ConfirmPassword = "DifferentPassword1!" };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void Validate_WhenPasswordsMatch_HasNoConfirmPasswordError()
    {
        _validator.TestValidate(ValidCommand).ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }
}
