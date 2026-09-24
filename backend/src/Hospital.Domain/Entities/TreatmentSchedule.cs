using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

/// <summary>
/// Single source of truth for which treatment is available on which weekday.
/// </summary>
public class TreatmentSchedule : BaseEntity
{
    public Guid TreatmentId { get; set; }
    public Treatment Treatment { get; set; } = null!;

    public Guid? TherapistId { get; set; }
    public Therapist? Therapist { get; set; }

    public Weekday DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxSlotsPerDay { get; set; }
    public bool IsActive { get; set; } = true;
}
