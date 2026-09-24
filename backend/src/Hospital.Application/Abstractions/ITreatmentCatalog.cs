namespace Hospital.Application.Abstractions;

/// <summary>
/// Read-only seam onto Member 2's treatment catalog.
/// Feedback only needs to know the treatment exists; it does not schedule or price therapies.
/// </summary>
public interface ITreatmentCatalog
{
    Task<bool> ExistsAsync(Guid treatmentId, CancellationToken cancellationToken);
}
