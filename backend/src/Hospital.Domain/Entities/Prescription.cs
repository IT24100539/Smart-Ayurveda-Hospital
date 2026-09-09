using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public class Prescription : BaseEntity
{
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public DateOnly ValidUntil { get; set; }
    public string? Notes { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
