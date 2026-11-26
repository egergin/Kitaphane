using Kitaphane.Application.Auth;

namespace Kitaphane.UnitTests;

public sealed class AuthValidatorTests
{
    private readonly RegisterRequestValidator _register = new();
    private readonly VerifyEmailRequestValidator _verifyEmail = new();

    [Theory]
    [InlineData("Password1", true)]
    [InlineData("abcdefg1", true)]
    [InlineData("şifre1234", true)]
    [InlineData("Pass1", false)]          // too short
    [InlineData("Passwordxx", false)]     // no digit
    [InlineData("12345678", false)]       // no letter
    [InlineData("", false)]
    public void Password_requires_eight_characters_with_letter_and_digit(string password, bool isValid)
    {
        var result = _register.Validate(new RegisterRequest("Ad Soyad", "user@example.com", password));

        Assert.Equal(isValid, result.IsValid);
    }

    [Theory]
    [InlineData("", "user@example.com")]
    [InlineData("Ad Soyad", "not-an-email")]
    [InlineData("Ad Soyad", "")]
    public void Register_requires_name_and_valid_email(string name, string email)
    {
        var result = _register.Validate(new RegisterRequest(name, email, "Password1"));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12a456", false)]
    [InlineData("", false)]
    public void Verification_code_must_be_six_digits(string code, bool isValid)
    {
        var result = _verifyEmail.Validate(new VerifyEmailRequest("user@example.com", code));

        Assert.Equal(isValid, result.IsValid);
    }
}
