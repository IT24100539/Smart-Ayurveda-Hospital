using FluentValidation;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Validators;
using Hospital.Application.StaffManagement.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.StaffManagement.Validators;

public sealed class CreateStaffUserRequestValidator : AbstractValidator<CreateStaffUserRequest>
{
    public CreateStaffUserRequestValidator()
        : this(new PasswordPolicyOptions())
    {
    }

    public CreateStaffUserRequestValidator(PasswordPolicyOptions policy)
    {
        var rules = policy.Normalized();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().MinimumLength(6).MaximumLength(20);
        RuleFor(x => x.Role).IsInEnum().Must(role => role != UserRole.Patient)
            .WithMessage("Staff accounts cannot use the Patient role.");
        RuleFor(x => x.TemporaryPassword!).ApplyPasswordPolicy(rules)
            .When(x => !string.IsNullOrWhiteSpace(x.TemporaryPassword));
    }
}

public sealed class UpdateStaffRoleRequestValidator : AbstractValidator<UpdateStaffRoleRequest>
{
    public UpdateStaffRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum().Must(role => role != UserRole.Patient)
            .WithMessage("Staff accounts cannot use the Patient role.");
    }
}

public sealed class UpdateStaffStatusRequestValidator : AbstractValidator<UpdateStaffStatusRequest>
{
}

public sealed class ForcePasswordResetRequestValidator : AbstractValidator<ForcePasswordResetRequest>
{
    public ForcePasswordResetRequestValidator()
        : this(new PasswordPolicyOptions())
    {
    }

    public ForcePasswordResetRequestValidator(PasswordPolicyOptions policy)
    {
        var rules = policy.Normalized();
        RuleFor(x => x.TemporaryPassword!).ApplyPasswordPolicy(rules)
            .When(x => !string.IsNullOrWhiteSpace(x.TemporaryPassword));
    }
}
