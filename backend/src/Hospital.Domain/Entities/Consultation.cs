using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Consultation : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public string ChiefComplaint { get; set; } = string.Empty;
    public string? History { get; set; }
    public string? NadiPariksha { get; set; }
    public string? JihvaPariksha { get; set; }
    public DoshaType AssessedDosha { get; set; } = DoshaType.None;
    public string? Diagnosis { get; set; }
    public string? LifestyleAdvice { get; set; }
    public string? DietAdvice { get; set; }

    public Prescription? Prescription { get; set; }
}
