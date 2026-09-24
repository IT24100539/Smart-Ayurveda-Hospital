namespace Hospital.Application.Treatments;

/// <summary>
/// Counts existing bookings for a treatment on a calendar date.
/// Used by <see cref="ITreatmentService.IsAvailableOnAsync"/> when computing remaining slots.
/// </summary>
/// <remarks>
/// TODO(Member 3): implement this against the Appointment table once treatment bookings
/// exist (filter by TreatmentId + date, exclude cancelled / no-show). Register the real
/// implementation in Infrastructure so it replaces <see cref="UnimplementedAppointmentCountProvider"/>.
/// Until then the schedule-only half of availability still works (booked count = 0).
/// </remarks>
public interface IAppointmentCountProvider
{
    Task<int> CountBookedSlotsAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken);
}

/// <summary>
/// Schedule-only stub. Remaining slots equal MaxSlotsPerDay until Member 3 wires appointments.
/// </summary>
public sealed class UnimplementedAppointmentCountProvider : IAppointmentCountProvider
{
    public Task<int> CountBookedSlotsAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}
