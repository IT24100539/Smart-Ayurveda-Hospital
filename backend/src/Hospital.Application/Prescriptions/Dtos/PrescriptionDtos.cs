using Hospital.Domain.Entities;

namespace Hospital.Application.Prescriptions.Dtos;

public sealed record PrescriptionItemRequest(
    string Name,
    string Dosage,
    string Frequency,
    string Duration,
    string? Instructions);

public sealed record CreatePrescriptionRequest(
    Guid PatientId,
    Guid AppointmentId,
    IReadOnlyList<PrescriptionItemRequest> Items);

public sealed record UpdatePrescriptionRequest(
    IReadOnlyList<PrescriptionItemRequest> Items);

public sealed record RevisePrescriptionRequest(
    string Reason,
    IReadOnlyList<PrescriptionItemRequest> Items);

public sealed record PrescriptionItemDto(
    Guid Id,
    string Name,
    string Dosage,
    string Frequency,
    string Duration,
    string Instructions);

public sealed record PrescriptionDto(
    Guid Id,
    Guid PatientId,
    Guid AppointmentId,
    Guid DoctorUserId,
    string DoctorName,
    PrescriptionStatus Status,
    int RevisionNumber,
    Guid RootPrescriptionId,
    Guid? RevisesPrescriptionId,
    Guid? SupersededByPrescriptionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? SupersededAt,
    IReadOnlyList<PrescriptionItemDto> Items);

public sealed record PrescriptionRevisionDto(
    Guid Id,
    Guid PreviousPrescriptionId,
    Guid RevisedPrescriptionId,
    int RevisionNumber,
    Guid RevisedByUserId,
    string Reason,
    DateTimeOffset RevisedAt);

public sealed record PrescriptionHistoryDto(
    Guid RootPrescriptionId,
    IReadOnlyList<PrescriptionDto> Versions,
    IReadOnlyList<PrescriptionRevisionDto> Revisions);
