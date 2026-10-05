using FluentValidation;
using Hospital.Application.Doctors.Dtos;
using Hospital.Domain.Entities;

namespace Hospital.Application.Doctors.Validators;

public sealed class CreateDoctorRequestValidator : AbstractValidator<CreateDoctorRequest>
{
    public CreateDoctorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Doctor.NameMaxLength);
        RuleFor(x => x.Specialty).NotEmpty().MaximumLength(Doctor.SpecialtyMaxLength);
        RuleFor(x => x.Qualifications).NotEmpty().MaximumLength(Doctor.QualificationsMaxLength);
        RuleFor(x => x.Bio).MaximumLength(Doctor.BioMaxLength);
    }
}

public sealed class UpdateDoctorRequestValidator : AbstractValidator<UpdateDoctorRequest>
{
    public UpdateDoctorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Doctor.NameMaxLength);
        RuleFor(x => x.Specialty).NotEmpty().MaximumLength(Doctor.SpecialtyMaxLength);
        RuleFor(x => x.Qualifications).NotEmpty().MaximumLength(Doctor.QualificationsMaxLength);
        RuleFor(x => x.Bio).MaximumLength(Doctor.BioMaxLength);
    }
}
