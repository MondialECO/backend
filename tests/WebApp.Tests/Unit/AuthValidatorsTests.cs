using FluentValidation.TestHelper;
using WebApp.Models;
using WebApp.Validation;
using Xunit;

namespace WebApp.Tests.Unit;

public class AuthValidatorsTests
{
    private readonly LoginRequestModelValidator _login = new();
    private readonly RegisterModelValidator _register = new();
    private readonly ResetPasswordRequestModelValidator _reset = new();

    [Fact]
    public void Login_requires_valid_email_and_password()
    {
        var result = _login.TestValidate(new LoginRequestModel { Email = "not-an-email", Password = "" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Login_passes_with_valid_input()
    {
        var result = _login.TestValidate(new LoginRequestModel { Email = "a@b.com", Password = "whatever" });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("short1A", false)]   // ok: >=6, upper, lower, digit
    [InlineData("alllowercase1", true)]   // no uppercase -> error
    [InlineData("NoDigitsHere", true)]    // no digit -> error
    [InlineData("abc1A", true)]           // too short -> error
    public void Register_enforces_password_policy(string password, bool expectError)
    {
        var result = _register.TestValidate(new RegisterModel
        {
            Name = "Jane",
            Email = "jane@example.com",
            Password = password,
            User = "Creator"
        });

        if (expectError)
            result.ShouldHaveValidationErrorFor(x => x.Password);
        else
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Reset_requires_matching_confirmation()
    {
        var result = _reset.TestValidate(new ResetPasswordRequestModel
        {
            Email = "a@b.com",
            Token = "tok",
            NewPassword = "Valid1Pass",
            ConfirmPassword = "Different1"
        });
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }
}
