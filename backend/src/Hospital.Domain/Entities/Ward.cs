using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Ward : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameSinhala { get; set; } = string.Empty;
    public WardGender Gender { get; set; }
    public int TotalCapacity { get; set; }

    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
    public ICollection<AdmissionRequest> AdmissionRequests { get; set; } = new List<AdmissionRequest>();
}
