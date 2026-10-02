using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public sealed class Prescription : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorUserId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Directives { get; set; } = string.Empty;
    public bool IsRefillable { get; set; } = true;
    public int RemainingRefills { get; set; } = 2;
    public DateTimeOffset PrescriptionDate { get; set; } = DateTimeOffset.UtcNow;
    public List<PrescriptionItem> Items { get; set; } = new();
}

public sealed class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public string HerbFormulationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty; // e.g., Twice daily after meals
    public int DurationDays { get; set; } = 7;
    public string SpecialInstructions { get; set; } = string.Empty;
}
