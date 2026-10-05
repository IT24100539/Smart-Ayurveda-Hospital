using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class PrescriptionRepository : IPrescriptionRepository
{
    private readonly HospitalDbContext _db;

    public PrescriptionRepository(HospitalDbContext db) => _db = db;

    public Task<Prescription?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = _db.Prescriptions.Include(x => x.Items).AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Prescription> Items, int Total)> ListAsync(
        Guid? patientId,
        Guid? appointmentId,
        bool includeDrafts,
        bool includeSuperseded,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Prescriptions.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (patientId is Guid ownerId)
        {
            query = query.Where(x => x.PatientId == ownerId);
        }

        if (appointmentId is Guid visitId)
        {
            query = query.Where(x => x.AppointmentId == visitId);
        }

        if (!includeDrafts)
        {
            query = query.Where(x => x.Status != PrescriptionStatus.Draft);
        }

        if (!includeSuperseded)
        {
            query = query.Where(x => x.Status != PrescriptionStatus.Superseded);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.RevisionNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> HasOpenForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        _db.Prescriptions.AnyAsync(
            x => x.AppointmentId == appointmentId
                && (x.Status == PrescriptionStatus.Draft || x.Status == PrescriptionStatus.Issued),
            cancellationToken);

    public async Task<IReadOnlyList<Prescription>> ListChainAsync(
        Guid rootPrescriptionId,
        CancellationToken cancellationToken) =>
        await _db.Prescriptions.AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.RootPrescriptionId == rootPrescriptionId)
            .OrderBy(x => x.RevisionNumber)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PrescriptionRevision>> ListRevisionsAsync(
        IReadOnlyCollection<Guid> prescriptionIds,
        CancellationToken cancellationToken)
    {
        if (prescriptionIds.Count == 0)
        {
            return Array.Empty<PrescriptionRevision>();
        }

        return await _db.PrescriptionRevisions.AsNoTracking()
            .Where(x => prescriptionIds.Contains(x.RevisedPrescriptionId))
            .OrderBy(x => x.RevisionNumber)
            .ThenBy(x => x.RevisedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Prescription prescription, CancellationToken cancellationToken) =>
        await _db.Prescriptions.AddAsync(prescription, cancellationToken);

    public async Task AddRevisionAsync(PrescriptionRevision revision, CancellationToken cancellationToken) =>
        await _db.PrescriptionRevisions.AddAsync(revision, cancellationToken);

    public void ReplaceItems(Prescription prescription, IReadOnlyList<PrescriptionItem> items)
    {
        if (prescription.Items.Count > 0)
        {
            _db.PrescriptionItems.RemoveRange(prescription.Items);
            prescription.Items.Clear();
        }

        foreach (var item in items)
        {
            item.PrescriptionId = prescription.Id;
            prescription.Items.Add(item);
        }

        _db.Entry(prescription).Property(x => x.UpdatedAt).IsModified = true;
    }
}
