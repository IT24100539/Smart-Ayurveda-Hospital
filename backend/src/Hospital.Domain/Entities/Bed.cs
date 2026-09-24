using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public class Bed : BaseEntity
{
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;

    /// <summary>Label within the ward, e.g. A-05.</summary>
    public string BedLabel { get; set; } = string.Empty;

    /// <summary>
    /// Denormalized occupancy flag for fast reads. Kept in sync via AdmissionRequest
    /// transitions; not edited directly by staff.
    /// </summary>
    public bool IsOccupied { get; set; }

    public ICollection<AdmissionRequest> AdmissionRequests { get; set; } = new List<AdmissionRequest>();
}
