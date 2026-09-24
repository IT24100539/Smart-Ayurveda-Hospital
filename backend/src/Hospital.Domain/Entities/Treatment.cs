using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Treatment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TreatmentCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
