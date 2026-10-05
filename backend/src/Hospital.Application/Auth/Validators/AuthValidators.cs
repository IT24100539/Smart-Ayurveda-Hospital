using FluentValidation;
using Hospital.Application.Auth.Dtos;

namespace Hospital.Application.Auth.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
        : this(new Hospital.Application.Auth.PasswordPolicyOptions())
    {
    }

    public ChangePasswordRequestValidator(Hospital.Application.Auth.PasswordPolicyOptions policy)
    {
        var rules = policy.Normalized();
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(rules.MaximumLength);
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy(rules);
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
        : this(new Hospital.Application.Auth.PasswordPolicyOptions())
    {
    }

    public ResetPasswordRequestValidator(Hospital.Application.Auth.PasswordPolicyOptions policy)
    {
        var rules = policy.Normalized();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy(rules);
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}
