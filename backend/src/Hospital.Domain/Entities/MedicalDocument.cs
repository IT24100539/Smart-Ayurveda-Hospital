using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public enum DocumentCategory
{
    LabReport,
    PrescriptionScan,
    DiagnosticScan,
    DischargeSummary,
    TreatmentPlan,
    General
}

public sealed class MedicalDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; } = DocumentCategory.General;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid UploadedByUserId { get; set; }
    public string Summary { get; set; } = string.Empty;
}
