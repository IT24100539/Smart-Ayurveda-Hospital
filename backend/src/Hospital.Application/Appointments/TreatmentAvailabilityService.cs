using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Appointments;

public sealed record TreatmentAvailabilityDto(Guid TreatmentId, DateOnly Date, bool Available, string? Reason = null);

public sealed record TreatmentSlotAvailabilityDto(
    Guid ScheduleId,
    string Time,
    bool Available,
    string? DoctorName);

public sealed record TreatmentDayAvailabilityDto(
    Guid TreatmentId,
    DateOnly Date,
    bool Available,
    string? Reason,
    IReadOnlyList<TreatmentSlotAvailabilityDto> Slots);

public sealed class TreatmentAvailabilityService(
    ITreatmentRepository treatments, IAppointmentRepository appointments, IBookingValidator validator)
{
    public async Task<TreatmentAvailabilityDto> GetAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken)
    {
        var day = await GetDayAsync(treatmentId, date, cancellationToken);
        return new(day.TreatmentId, day.Date, day.Available, day.Reason);
    }

    public async Task<TreatmentDayAvailabilityDto> GetDayAsync(
        Guid treatmentId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var treatment = await treatments.GetByIdAsync(treatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), treatmentId);
        if (!treatment.IsActive)
        {
            return new(treatmentId, date, false, "Treatment is inactive.", Array.Empty<TreatmentSlotAvailabilityDto>());
        }
        var schedules = await treatments.ListSchedulesAsync(treatmentId, cancellationToken);
        var slots = new List<TreatmentSlotAvailabilityDto>();
        foreach (var schedule in schedules)
        {
            var slot = schedule.SlotLabel;
            try
            {
                validator.Validate(schedule, date, slot);
            }
            catch (DomainException)
            {
                continue;
            }

            var booked = await appointments.CountActiveAppointmentsAsync(
                treatmentId,
                date,
                slot,
                cancellationToken);
            var capacity = schedule.MaxPatients > 0 ? schedule.MaxPatients : schedule.MaxSlotsPerDay;
            var available = capacity <= 0 || booked < capacity;
            slots.Add(new(
                schedule.Id,
                slot,
                available,
                schedule.Therapist?.FullName));
        }

        if (slots.Count == 0)
        {
            return new(treatmentId, date, false, "No matching schedule with remaining capacity.", slots);
        }

        var open = slots.Exists(slot => slot.Available);
        return new(treatmentId, date, open, open ? null : "Selected time slot is full.", slots);
    }
}
