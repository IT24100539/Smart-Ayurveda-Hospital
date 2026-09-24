using FluentValidation;
using Hospital.Application.Appointments.Dtos;

namespace Hospital.Application.Appointments.Validators;

public sealed class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.TreatmentId).NotEmpty();
        RuleFor(x => x.RequestedDate).NotEqual(default(DateOnly));
        RuleFor(x => x.RequestedTimeSlot).NotEmpty().MaximumLength(32);
    }
}

public sealed class UpdateAppointmentStatusRequestValidator : AbstractValidator<UpdateAppointmentStatusRequest>
{
    public UpdateAppointmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
