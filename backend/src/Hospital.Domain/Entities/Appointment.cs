using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid TreatmentId { get; set; }
    public Treatment Treatment { get; set; } = null!;

    public Guid? ScheduleId { get; set; }
    public TreatmentSchedule? Schedule { get; set; }

    public DateOnly RequestedDate { get; set; }
    public string RequestedTimeSlot { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public Guid? DecidedById { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }

    public Consultation? Consultation { get; set; }
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
