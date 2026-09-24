using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

/// <summary>
/// Single source of truth for which treatment is available on which weekday.
/// Appointments reference this schedule so Appointment.ScheduleId is a real FK.
/// </summary>
public class TreatmentSchedule : BaseEntity
{
    public Guid TreatmentId { get; set; }
    public Treatment Treatment { get; set; } = null!;

    public Guid? TherapistId { get; set; }
    public Therapist? Therapist { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int MaxSlotsPerDay { get; set; }
    public int MaxPatients { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}