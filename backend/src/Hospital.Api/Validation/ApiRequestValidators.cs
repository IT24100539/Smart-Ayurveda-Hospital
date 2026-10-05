using FluentValidation;
using Hospital.Api.Controllers;
using Hospital.Application.Common;

namespace Hospital.Api.Validation;

public sealed class AppointmentDecisionRequestValidator : AbstractValidator<AppointmentDecisionRequest>
{
    public AppointmentDecisionRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.DecidedBy!.Value).NotEmpty().When(x => x.DecidedBy is not null);
    }
}

public sealed class InternalAdmissionRequestValidator : AbstractValidator<InternalAdmissionRequest>
{
    public InternalAdmissionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CheckSlotRequestValidator : AbstractValidator<CheckSlotRequest>
{
    public CheckSlotRequestValidator()
    {
        RuleFor(x => x.RequestedTimeSlot).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.RequestedTimeSlot));
        RuleFor(x => x.RequestedDate!.Value)
            .Must(DateSanity.IsCalendarDate)
            .When(x => x.RequestedDate is not null)
            .WithMessage("Requested date is outside the supported range.");
        RuleFor(x => x.PatientId!.Value).NotEmpty().When(x => x.PatientId is not null);
        RuleFor(x => x.TreatmentId!.Value).NotEmpty().When(x => x.TreatmentId is not null);
    }
}
