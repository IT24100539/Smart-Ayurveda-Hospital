using FluentValidation;
using Hospital.Application.Auth.Dtos;


namespace Hospital.Application.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
        : this(new Hospital.Application.Auth.PasswordPolicyOptions())
    {
    }

    public LoginRequestValidator(Hospital.Application.Auth.PasswordPolicyOptions policy)
    {
        var rules = policy.Normalized();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(rules.MinimumLength)
            .WithMessage($"Password must be at least {rules.MinimumLength} characters long.")
            .MaximumLength(rules.MaximumLength)
            .WithMessage($"Password cannot exceed {rules.MaximumLength} characters.");
    }
}
