using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

/// <summary>
/// Drafts can still be changed. After issue, the written text stays put.
/// A later change is a new issued row, and this one becomes superseded.
/// </summary>
public enum PrescriptionStatus
{
    Draft = 1,
    Issued = 2,
    Superseded = 3,
    Cancelled = 4
}

public sealed class Prescription : BaseEntity
{
    public const int DoctorNameMaxLength = 160;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid DoctorUserId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public int RevisionNumber { get; set; } = 1;

    /// <summary>First version in this chain. Equals <see cref="BaseEntity.Id"/> on the original.</summary>
    public Guid RootPrescriptionId { get; set; }

    public Guid? RevisesPrescriptionId { get; set; }
    public Prescription? Revises { get; set; }

    public Guid? SupersededByPrescriptionId { get; set; }
    public Prescription? SupersededBy { get; set; }

    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? SupersededAt { get; set; }

    public List<PrescriptionItem> Items { get; set; } = new();
}

public sealed class PrescriptionItem : BaseEntity
{
    public const int NameMaxLength = 160;
    public const int DosageMaxLength = 120;
    public const int FrequencyMaxLength = 120;
    public const int DurationMaxLength = 80;
    public const int InstructionsMaxLength = 500;
    public const int MaxItems = 30;

    public Guid PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }

    public int SortOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

/// <summary>
/// One recorded change from an issued prescription to the next version.
/// The previous prescription row is kept as written.
/// </summary>
public sealed class PrescriptionRevision : BaseEntity
{
    public const int ReasonMaxLength = 500;

    public Guid PreviousPrescriptionId { get; set; }
    public Prescription? PreviousPrescription { get; set; }

    public Guid RevisedPrescriptionId { get; set; }
    public Prescription? RevisedPrescription { get; set; }

    public int RevisionNumber { get; set; }
    public Guid RevisedByUserId { get; set; }
    public DateTimeOffset RevisedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}
