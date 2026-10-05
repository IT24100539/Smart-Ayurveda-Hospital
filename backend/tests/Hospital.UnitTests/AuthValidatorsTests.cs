using FluentValidation;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Dtos;
using Hospital.Application.Auth.Validators;
using Hospital.Domain.Enums;

namespace Hospital.UnitTests;

public sealed class AuthValidatorsTests
{
    [Fact]
    public void CompleteReset_WeakNewPassword_IsRejected()
    {
        var validator = new ResetPasswordRequestValidator();
        var result = validator.Validate(new ResetPasswordRequest(
            "reset@example.com",
            "token",
            "weak",
            "weak"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ResetPasswordRequest.NewPassword));
    }

    [Fact]
    public void CompleteReset_StrongNewPassword_IsAccepted()
    {
        var validator = new ResetPasswordRequestValidator();
        var result = validator.Validate(new ResetPasswordRequest(
            "reset@example.com",
            "token",
            "NewPass!2345",
            "NewPass!2345"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_UsesConfiguredLengthAndComplexity()
    {
        var strictLength = new RegisterRequestValidator(new PasswordPolicyOptions
        {
            MinimumLength = 12,
            RequireUppercase = false
        });

        var tooShort = strictLength.Validate(new RegisterRequest(
            "Nimal Silva",
            "nimal@example.com",
            "0771234567",
            "abcdefg1!"));
        Assert.False(tooShort.IsValid);
        Assert.Contains(tooShort.Errors, e => e.ErrorMessage.Contains("at least 12 characters", StringComparison.Ordinal));
        Assert.DoesNotContain(tooShort.Errors, e => e.ErrorMessage.Contains("uppercase", StringComparison.Ordinal));

        var accepted = strictLength.Validate(new RegisterRequest(
            "Nimal Silva",
            "nimal@example.com",
            "0771234567",
            "abcdefg1!xyz",
            DateOfBirth: new DateOnly(1992, 3, 4),
            Gender: Gender.Male));
        Assert.True(accepted.IsValid);
    }
}
