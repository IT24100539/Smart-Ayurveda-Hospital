using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public interface IMedicalDocumentRepository
{
    Task<MedicalDocument?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);

    Task<(IReadOnlyList<MedicalDocument> Items, int Total)> ListByPatientAsync(
        Guid patientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddAsync(MedicalDocument document, CancellationToken cancellationToken);
}

public sealed record StoredMedicalDocument(string StorageKey, string ContentType, long SizeBytes);

/// <summary>
/// Writes clinical files under a directory outside the web root. Names are generated here.
/// The uploaded file name is never part of the storage key.
/// </summary>
public interface IMedicalDocumentStore
{
    Task<StoredMedicalDocument> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a previously generated storage key. Returns null when the key is missing,
    /// malformed, or would resolve outside the storage directory.
    /// </summary>
    Stream? OpenRead(string storageKey);

    Task DeleteAsync(string? storageKey, CancellationToken cancellationToken);
}
