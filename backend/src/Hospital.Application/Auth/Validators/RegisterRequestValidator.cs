using FluentValidation;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password).ApplyPasswordPolicy();
        RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue);
        RuleFor(x => x.DateOfBirth)
            .NotNull()
            .When(x => IsPatientRegistration(x));
        RuleFor(x => x.DateOfBirth!.Value)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
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
