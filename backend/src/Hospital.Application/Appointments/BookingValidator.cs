using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Appointments;

public sealed class BookingValidator : IBookingValidator
{
    public void Validate(TreatmentSchedule schedule, DateOnly requestedDate, string requestedTimeSlot)
    {
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));
        if (!schedule.IsActive)
        {
            throw new DomainException("Selected schedule is not active.");
        }

        if (requestedDate.DayOfWeek != schedule.DayOfWeek)
        {
            throw new DomainException("Requested date does not match schedule day of week.");
        }

        if (!string.Equals(schedule.TimeSlot?.Trim(), requestedTimeSlot?.Trim(), StringComparison.Ordinal))
        {
            throw new DomainException("Requested time slot does not match schedule time slot.");
        }
    }
}
