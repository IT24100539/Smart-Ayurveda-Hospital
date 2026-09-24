using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class AdmissionRequest : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    /// <summary>Null until a ward is assigned.</summary>
    public Guid? WardId { get; set; }
    public Ward? Ward { get; set; }

    /// <summary>Null until the request is approved with a bed.</summary>
    public Guid? BedId { get; set; }
    public Bed? Bed { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateOnly PreferredDate { get; set; }
    public AdmissionRequestStatus Status { get; set; } = AdmissionRequestStatus.Pending;

    /// <summary>True when raised via the Scheduling &amp; Bed Agent rather than directly by staff.</summary>
    public bool RequestedByAgent { get; set; }

    public Guid? DecidedBy { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}
