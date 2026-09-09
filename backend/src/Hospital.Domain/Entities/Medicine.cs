using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Medicine : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public MedicineForm Form { get; set; }
    public string? Manufacturer { get; set; }
    public string DosageGuidelines { get; set; } = string.Empty;
    public string? Contraindications { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
