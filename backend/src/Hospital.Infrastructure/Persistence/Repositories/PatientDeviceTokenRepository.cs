using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class PatientDeviceTokenRepository : IPatientDeviceTokenRepository
{
    private readonly HospitalDbContext _db;

    public PatientDeviceTokenRepository(HospitalDbContext db) => _db = db;

    public Task<PatientDeviceToken?> FindByTokenAsync(string token, CancellationToken cancellationToken) =>
        _db.PatientDeviceTokens.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public async Task<IReadOnlyList<string>> ListTokensForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        await _db.PatientDeviceTokens.AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .Select(x => x.Token)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PatientDeviceToken deviceToken, CancellationToken cancellationToken) =>
        await _db.PatientDeviceTokens.AddAsync(deviceToken, cancellationToken);
}
