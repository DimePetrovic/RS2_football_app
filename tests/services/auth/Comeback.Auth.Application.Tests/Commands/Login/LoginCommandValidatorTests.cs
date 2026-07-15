namespace Comeback.Auth.Application.Tests.Commands.Login;

using Comeback.Auth.Application.Features.Auth.Commands.Login;
using FluentValidation.TestHelper;
using Xunit;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    private static LoginCommand ValidCommand => new("player@comeback.com", "Password123!", "127.0.0.1");

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@nodomain")]
    public void Validate_WhenEmailIsInvalid_HasEmailError(string email)
    {
        var command = ValidCommand with { Email = email };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhenPasswordIsEmpty_HasPasswordError()
    {
        var command = ValidCommand with { Password = "" };
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Password);
    }
}
