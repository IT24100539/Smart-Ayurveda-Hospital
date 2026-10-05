using FluentValidation;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;

namespace Hospital.Application.Appointments.Validators;

public sealed class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.TreatmentId).NotEmpty();
        RuleFor(x => x.ScheduleId!.Value).NotEmpty().When(x => x.ScheduleId is not null);
        RuleFor(x => x.RequestedDate).Must(DateSanity.IsCalendarDate).WithMessage("Requested date is outside the supported range.");
        RuleFor(x => x.RequestedTimeSlot).NotEmpty().MaximumLength(32);
    }
}

public sealed class UpdateAppointmentStatusRequestValidator : AbstractValidator<UpdateAppointmentStatusRequest>
{
    public UpdateAppointmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.DecidedBy!.Value).NotEmpty().When(x => x.DecidedBy is not null);
    }
}

public sealed class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.RequestedDate).Must(DateSanity.IsCalendarDate).WithMessage("Requested date is outside the supported range.");
        RuleFor(x => x.RequestedTimeSlot).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ScheduleId!.Value).NotEmpty().When(x => x.ScheduleId is not null);
    }
}
