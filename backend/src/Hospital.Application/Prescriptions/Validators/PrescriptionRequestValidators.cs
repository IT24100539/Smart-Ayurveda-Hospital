using FluentValidation;
using Hospital.Application.Prescriptions.Dtos;
using Hospital.Domain.Entities;

namespace Hospital.Application.Prescriptions.Validators;

public sealed class PrescriptionItemRequestValidator : AbstractValidator<PrescriptionItemRequest>
{
    public PrescriptionItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(PrescriptionItem.NameMaxLength);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(PrescriptionItem.DosageMaxLength);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(PrescriptionItem.FrequencyMaxLength);
        RuleFor(x => x.Duration).NotEmpty().MaximumLength(PrescriptionItem.DurationMaxLength);
        RuleFor(x => x.Instructions).MaximumLength(PrescriptionItem.InstructionsMaxLength);
    }
}

public sealed class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Items).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(items => items.Count <= PrescriptionItem.MaxItems)
            .WithMessage($"A prescription can include at most {PrescriptionItem.MaxItems} items.");
        RuleForEach(x => x.Items)
            .SetValidator(new PrescriptionItemRequestValidator())
            .When(x => x.Items is not null);
    }
}

public sealed class UpdatePrescriptionRequestValidator : AbstractValidator<UpdatePrescriptionRequest>
{
    public UpdatePrescriptionRequestValidator()
    {
        RuleFor(x => x.Items).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(items => items.Count <= PrescriptionItem.MaxItems)
            .WithMessage($"A prescription can include at most {PrescriptionItem.MaxItems} items.");
        RuleForEach(x => x.Items)
            .SetValidator(new PrescriptionItemRequestValidator())
            .When(x => x.Items is not null);
    }
}

public sealed class RevisePrescriptionRequestValidator : AbstractValidator<RevisePrescriptionRequest>
{
    public RevisePrescriptionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(PrescriptionRevision.ReasonMaxLength);
        RuleFor(x => x.Items).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(items => items.Count <= PrescriptionItem.MaxItems)
            .WithMessage($"A prescription can include at most {PrescriptionItem.MaxItems} items.");
        RuleForEach(x => x.Items)
            .SetValidator(new PrescriptionItemRequestValidator())
            .When(x => x.Items is not null);
    }
}
