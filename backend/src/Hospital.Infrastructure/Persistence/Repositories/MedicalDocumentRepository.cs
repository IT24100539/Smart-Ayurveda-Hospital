using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class MedicalDocumentRepository : IMedicalDocumentRepository
{
    private readonly HospitalDbContext _db;

    public MedicalDocumentRepository(HospitalDbContext db) => _db = db;

    public Task<MedicalDocument?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = _db.MedicalDocuments.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<MedicalDocument> Items, int Total)> ListByPatientAsync(
        Guid patientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.MedicalDocuments.AsNoTracking().Where(x => x.PatientId == patientId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.UploadedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(MedicalDocument document, CancellationToken cancellationToken) =>
        await _db.MedicalDocuments.AddAsync(document, cancellationToken);
}
