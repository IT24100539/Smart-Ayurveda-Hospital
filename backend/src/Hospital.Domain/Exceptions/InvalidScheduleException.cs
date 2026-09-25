namespace Hospital.Domain.Exceptions;

/// <summary>
/// Thrown when a booking is attempted on a date with no matching active treatment schedule.
/// Mapped to HTTP 400 by the API exception middleware.
/// </summary>
public sealed class InvalidScheduleException : DomainException
{
    public Guid TreatmentId { get; }
    public DateOnly Date { get; }

    public InvalidScheduleException(Guid treatmentId, DateOnly date)
        : base($"Treatment '{treatmentId}' has no active schedule on {date:yyyy-MM-dd} ({date.DayOfWeek}).")
    {
        TreatmentId = treatmentId;
        Date = date;
    }

    public InvalidScheduleException(string message) : base(message)
    {
        Date = default;
    }
}
