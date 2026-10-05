using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public sealed record DoctorRatingSummary(decimal Average, int Count);

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Doctor> Items, int Total)> ListAsync(
        string? query,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task AddAsync(Doctor doctor, CancellationToken cancellationToken);

    /// <summary>
    /// Average of visible feedback whose appointment is completed and assigned to the physician.
    /// Physicians with no such feedback are absent from the dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, DoctorRatingSummary>> GetRatingsAsync(
        IReadOnlyCollection<Guid> doctorIds,
        CancellationToken cancellationToken);
}

public sealed record StoredDoctorPhoto(string StorageKey, string ContentType, long SizeBytes);

public interface IDoctorPhotoStore
{
    Task<StoredDoctorPhoto> SaveAsync(Guid doctorId, Stream content, string contentType, CancellationToken cancellationToken);
    Stream? OpenRead(string storageKey);
    Task DeleteAsync(string? storageKey, CancellationToken cancellationToken);
}
