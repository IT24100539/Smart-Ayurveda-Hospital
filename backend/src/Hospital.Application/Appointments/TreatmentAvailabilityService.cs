using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Appointments;

public sealed record TreatmentAvailabilityDto(Guid TreatmentId, DateOnly Date, bool Available, string? Reason = null);

public sealed class TreatmentAvailabilityService(
    ITreatmentRepository treatments, IAppointmentRepository appointments, IBookingValidator validator)
{
    public async Task<TreatmentAvailabilityDto> GetAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken)
    {
        _ = await treatments.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), treatmentId);
        var schedules = await treatments.ListSchedulesAsync(treatmentId, cancellationToken);
        foreach (var schedule in schedules)
        {
            try { validator.Validate(schedule, date, schedule.TimeSlot); }
            catch (DomainException) { continue; }

            // Same unlimited-capacity convention and active booking count as appointment creation.
            if (schedule.MaxPatients <= 0 ||
                await appointments.CountActiveAppointmentsAsync(treatmentId, date, schedule.TimeSlot.Trim(), cancellationToken) < schedule.MaxPatients)
                return new(treatmentId, date, true);
        }
        return new(treatmentId, date, false, "No matching schedule with remaining capacity.");
    }
}
