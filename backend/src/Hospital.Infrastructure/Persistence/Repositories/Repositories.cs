using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class PatientRepository : IPatientRepository
{
    private readonly HospitalDbContext _db;

    public PatientRepository(HospitalDbContext db) => _db = db;

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        _db.Patients.FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);

    public Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Patients.FirstOrDefaultAsync(
            x => x.Email != null && x.Email.ToLower() == normalized,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var q = _db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            q = q.Where(x =>
                x.Uhid.ToLower().Contains(term) ||
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.Phone.Contains(term));
        }


        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken) =>
        await _db.Patients.AddAsync(patient, cancellationToken);
}

public sealed class UserRepository : IUserRepository
{
    private readonly HospitalDbContext _db;

    public UserRepository(HospitalDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<User?> FindActiveByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
        _db.Users
            .Where(x => x.IsActive && x.Role == role)
            .OrderBy(x => x.Email)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await _db.Users.AddAsync(user, cancellationToken);
}

public sealed class StaffUserRepository : IStaffUserRepository
{
    private readonly HospitalDbContext _db;

    public StaffUserRepository(HospitalDbContext db) => _db = db;

    public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.StaffUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.StaffUsers.FirstOrDefaultAsync(x => x.Email.ToLower() == normalized, cancellationToken);
    }

    public Task<StaffUser?> FindActiveByRoleAsync(StaffRole role, CancellationToken cancellationToken) =>
        _db.StaffUsers
            .Where(x => x.IsActive && x.Role == role)
            .OrderBy(x => x.Email)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<StaffUser>> ListActiveAsync(CancellationToken cancellationToken) =>
        await _db.StaffUsers.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
}

public sealed class TreatmentRepository : ITreatmentRepository
{
    private readonly HospitalDbContext _db;

    public TreatmentRepository(HospitalDbContext db) => _db = db;

    public async Task<IReadOnlyList<TreatmentSchedule>> ListSchedulesAsync(Guid treatmentId, CancellationToken cancellationToken) =>
        await _db.TreatmentSchedules.AsNoTracking().Where(x => x.TreatmentId == treatmentId).ToListAsync(cancellationToken);

    public Task<Treatment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Treatments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<TreatmentSchedule?> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.TreatmentSchedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly HospitalDbContext _db;

    public AppointmentRepository(HospitalDbContext db) => _db = db;

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Appointments
            .Include(x => x.Patient)
            .Include(x => x.Treatment)
            .Include(x => x.Schedule)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
        DateOnly? onDate,
        Guid? patientId,
        Guid? treatmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var q = _db.Appointments.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Treatment)
            .AsQueryable();

        if (onDate is not null)
        {
            q = q.Where(x => x.RequestedDate == onDate);
        }

        if (patientId is not null)
        {
            q = q.Where(x => x.PatientId == patientId);
        }

        if (treatmentId is not null)
        {
            q = q.Where(x => x.TreatmentId == treatmentId);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(x => x.RequestedDate)
            .ThenBy(x => x.RequestedTimeSlot)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> HasActiveSlotAsync(
        Guid patientId,
        Guid treatmentId,
        DateOnly requestedDate,
        string requestedTimeSlot,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _db.Appointments.AnyAsync(x =>
            x.PatientId == patientId &&
            x.TreatmentId == treatmentId &&
            x.RequestedDate == requestedDate &&
            x.RequestedTimeSlot == requestedTimeSlot &&
            x.Status != Domain.Enums.AppointmentStatus.Cancelled &&
            (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<int> CountActiveAppointmentsAsync(Guid treatmentId, DateOnly requestedDate, string requestedTimeSlot, CancellationToken cancellationToken) =>
        _db.Appointments.CountAsync(x =>
            x.TreatmentId == treatmentId &&
            x.RequestedDate == requestedDate &&
            x.RequestedTimeSlot == requestedTimeSlot &&
            x.Status != Domain.Enums.AppointmentStatus.Cancelled,
            cancellationToken);

    public async Task<bool> TryAddWithinCapacityAsync(Appointment appointment, int maxPatients, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("LOCK TABLE appointments IN SHARE ROW EXCLUSIVE MODE", cancellationToken);

        var activeCount = await CountActiveAppointmentsAsync(
            appointment.TreatmentId,
            appointment.RequestedDate,
            appointment.RequestedTimeSlot,
            cancellationToken);
        if (activeCount >= maxPatients)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        await _db.Appointments.AddAsync(appointment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken) =>
        await _db.Appointments.AddAsync(appointment, cancellationToken);
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly HospitalDbContext _db;

    public EfUnitOfWork(HospitalDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}

public sealed class WardRepository : IWardRepository
{
    private readonly HospitalDbContext _db;

    public WardRepository(HospitalDbContext db) => _db = db;

    public Task<Ward?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Wards.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<Ward?> GetWithBedsAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Wards.Include(w => w.Beds).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ward>> ListAllAsync(CancellationToken cancellationToken) =>
        await _db.Wards.Include(w => w.Beds).AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAdmissionRequestAsync(AdmissionRequest request, CancellationToken cancellationToken)
    {
        await _db.AdmissionRequests.AddAsync(request, cancellationToken);
    }

    public Task<AdmissionRequest?> GetAdmissionRequestByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.AdmissionRequests
            .Include(ar => ar.Bed)
            .Include(ar => ar.Ward)
            .Include(ar => ar.Patient)
            .FirstOrDefaultAsync(ar => ar.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AdmissionRequest>> ListPendingAdmissionsAsync(CancellationToken cancellationToken) =>
        await _db.AdmissionRequests
            .Where(ar => ar.Status == AdmissionRequestStatus.Pending)
            .Include(ar => ar.Patient)
            .Include(ar => ar.Ward)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<bool> TryApproveAdmissionAssignBedAsync(Guid admissionRequestId, Guid decidedBy, DateTimeOffset decidedAt, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var req = await _db.AdmissionRequests.Include(ar => ar.Ward).FirstOrDefaultAsync(ar => ar.Id == admissionRequestId, cancellationToken);
        if (req == null) return false;
        if (req.Status != AdmissionRequestStatus.Pending) return false;

        var wardId = req.WardId ?? throw new InvalidOperationException("Ward must be assigned for approval.");
        var freeBed = await _db.Beds
            .FromSqlInterpolated($"SELECT * FROM beds WHERE \"WardId\" = {wardId} AND NOT \"IsOccupied\" ORDER BY \"BedLabel\" FOR UPDATE SKIP LOCKED")
            .FirstOrDefaultAsync(cancellationToken);
        if (freeBed == null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        freeBed.IsOccupied = true;
        req.BedId = freeBed.Id;
        req.Status = AdmissionRequestStatus.Approved;
        req.DecidedBy = decidedBy;
        req.DecidedAt = decidedAt;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}
