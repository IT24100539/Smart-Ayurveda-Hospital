using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Prescription> Items, int Total)> ListAsync(
        Guid? patientId,
        Guid? appointmentId,
        bool includeDrafts,
        bool includeSuperseded,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> HasOpenForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Prescription>> ListChainAsync(Guid rootPrescriptionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrescriptionRevision>> ListRevisionsAsync(
        IReadOnlyCollection<Guid> prescriptionIds,
        CancellationToken cancellationToken);

    Task AddAsync(Prescription prescription, CancellationToken cancellationToken);

    Task AddRevisionAsync(PrescriptionRevision revision, CancellationToken cancellationToken);

    void ReplaceItems(Prescription prescription, IReadOnlyList<PrescriptionItem> items);
}
