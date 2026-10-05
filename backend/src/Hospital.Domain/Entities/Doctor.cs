using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

/// <summary>
/// Public physician profile. This is not a login account; staff sign-in stays on <see cref="User"/>.
/// A rating is never stored here. It is computed from visible feedback on completed appointments.
/// </summary>
public sealed class Doctor : BaseEntity
{
    public const int NameMaxLength = 160;
    public const int SpecialtyMaxLength = 160;
    public const int QualificationsMaxLength = 400;
    public const int BioMaxLength = 2000;
    public const string SampleNamePrefix = "Sample: ";

    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Development seed rows are sample profiles, not practising physicians.</summary>
    public bool IsSample { get; set; }

    /// <summary>File name under the photo store. Never a client path and never a public URL.</summary>
    public string? PhotoStorageKey { get; set; }
    public string? PhotoContentType { get; set; }
    public long? PhotoSizeBytes { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
