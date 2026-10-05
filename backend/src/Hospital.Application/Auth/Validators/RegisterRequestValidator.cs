using FluentValidation;
using Hospital.Application.Auth.Dtos;
using Hospital.Application.Common;
using Hospital.Domain.Enums;

namespace Hospital.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
        : this(new Hospital.Application.Auth.PasswordPolicyOptions())
    {
    }

    public RegisterRequestValidator(Hospital.Application.Auth.PasswordPolicyOptions policy)
    {
        var rules = policy.Normalized();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().MinimumLength(6).MaximumLength(20);
        RuleFor(x => x.Password).ApplyPasswordPolicy(rules);
        RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue);
        RuleFor(x => x.DateOfBirth)
            .NotNull()
            .When(x => IsPatientRegistration(x));
        RuleFor(x => x.DateOfBirth!.Value)
            .Must(DateSanity.IsBirthDate)
            .WithMessage("Date of birth must be between 1900 and today.")
            .When(x => x.DateOfBirth.HasValue);
        RuleFor(x => x.Gender)
            .NotNull()
            .When(x => IsPatientRegistration(x));
        RuleFor(x => x.Gender)
            .IsInEnum()
            .When(x => x.Gender.HasValue);
    }

    private static bool IsPatientRegistration(RegisterRequest request) =>
        request.Role is null || request.Role == UserRole.Patient;
}
