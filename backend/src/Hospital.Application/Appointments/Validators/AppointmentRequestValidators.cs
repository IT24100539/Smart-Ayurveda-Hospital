using FluentValidation;
using Hospital.Application.Appointments.Dtos;

namespace Hospital.Application.Appointments.Validators;

public sealed class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ScheduledAt).Must(dt => dt > DateTimeOffset.UtcNow.AddMinutes(-5))
            .WithMessage("Appointment must be scheduled in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 240);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateAppointmentStatusRequestValidator : AbstractValidator<UpdateAppointmentStatusRequest>
{
    public UpdateAppointmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
