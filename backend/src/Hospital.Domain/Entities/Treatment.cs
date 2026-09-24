using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Treatment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameSinhala { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionSinhala { get; set; } = string.Empty;
    public TreatmentCategory Category { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Session length used when booking a slot. Retained from the shared catalog.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>List price for invoicing. Retained from the shared catalog.</summary>
    public decimal UnitPrice { get; set; }
public bool IsActive { get; set; } = true;

public ICollection<TreatmentSchedule> Schedules { get; set; } = new List<TreatmentSchedule>();
public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
