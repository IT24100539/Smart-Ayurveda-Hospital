using Hospital.Domain.Entities;

namespace Hospital.Application.Appointments;

public interface IBookingValidator
{
    /// <summary>
    /// Validate that the requested date and time slot match the provided schedule.
    /// Throws DomainException on invalid booking.
    /// </summary>
    void Validate(TreatmentSchedule schedule, DateOnly requestedDate, string requestedTimeSlot);
}
