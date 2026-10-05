using FluentValidation;
using Hospital.Application.Common;
using Hospital.Application.Patients.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.Patients.Validators;

public sealed class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Phone).NotEmpty().MinimumLength(6).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.BloodGroup).MaximumLength(8);
        RuleFor(x => x.Allergies).MaximumLength(1000);
        RuleFor(x => x.DateOfBirth).Must(DateSanity.IsBirthDate).WithMessage("Date of birth must be between 1900 and today.");
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Prakriti).Must(DoshaRules.IsKnown).WithMessage("Prakriti is not a recognised dosha.");
        RuleFor(x => x.Vikriti).Must(DoshaRules.IsKnown).WithMessage("Vikriti is not a recognised dosha.");
    }
}

public sealed class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Phone).NotEmpty().MinimumLength(6).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.BloodGroup).MaximumLength(8);
        RuleFor(x => x.Allergies).MaximumLength(1000);
        RuleFor(x => x.DateOfBirth).Must(DateSanity.IsBirthDate).WithMessage("Date of birth must be between 1900 and today.");
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Prakriti).Must(DoshaRules.IsKnown).WithMessage("Prakriti is not a recognised dosha.");
        RuleFor(x => x.Vikriti).Must(DoshaRules.IsKnown).WithMessage("Vikriti is not a recognised dosha.");
    }
}

internal static class DoshaRules
{
    public static bool IsKnown(DoshaType dosha) => ((int)dosha & ~7) == 0;
}
