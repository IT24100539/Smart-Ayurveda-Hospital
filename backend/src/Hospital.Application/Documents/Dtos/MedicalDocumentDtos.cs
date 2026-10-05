using Hospital.Domain.Entities;

namespace Hospital.Application.Documents.Dtos;

public sealed record UploadMedicalDocumentRequest(
    Guid PatientId,
    string Title,
    DocumentCategory Category,
    string? Summary);

public sealed record MedicalDocumentDto(
    Guid Id,
    Guid PatientId,
    string Title,
    DocumentCategory Category,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset UploadedAt,
    string FileUrl,
    string Summary);

public sealed class MedicalDocumentFile
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string DownloadName { get; init; }
}
