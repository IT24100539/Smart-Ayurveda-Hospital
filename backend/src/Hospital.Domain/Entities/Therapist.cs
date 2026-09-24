using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public class Therapist : BaseEntity
{
    /// <summary>Optional staff/patient-app login. A therapist may practise without a User account.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    public ICollection<TreatmentSchedule> Schedules { get; set; } = new List<TreatmentSchedule>();
}
