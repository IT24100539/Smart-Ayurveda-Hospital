using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

/// <summary>
/// Recurring availability for a treatment. Owned by treatment information (Member 2);
/// present so Appointment.ScheduleId can be a real FK when that work is not yet on develop.
/// </summary>
public class TreatmentSchedule : BaseEntity
{
    public Guid TreatmentId { get; set; }
    public Treatment Treatment { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int MaxPatients { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
