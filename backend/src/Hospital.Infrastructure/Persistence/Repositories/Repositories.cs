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

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly HospitalDbContext _db;

    public AppointmentRepository(HospitalDbContext db) => _db = db;

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Appointments
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
        DateOnly? onDate,
        Guid? patientId,
        Guid? doctorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var q = _db.Appointments.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .AsQueryable();

        if (onDate is not null)
        {
            q = q.Where(x => DateOnly.FromDateTime(x.ScheduledAt.UtcDateTime) == onDate);
        }

        if (patientId is not null)
        {
            q = q.Where(x => x.PatientId == patientId);
        }

        if (doctorId is not null)
        {
            q = q.Where(x => x.DoctorId == doctorId);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(x => x.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> HasOverlapAsync(
        Guid doctorId,
        DateTimeOffset start,
        DateTimeOffset end,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _db.Appointments.AnyAsync(x =>
            x.DoctorId == doctorId &&
            x.Status != Domain.Enums.AppointmentStatus.Cancelled &&
            x.Status != Domain.Enums.AppointmentStatus.NoShow &&
            (excludeId == null || x.Id != excludeId) &&
            x.ScheduledAt < end &&
            x.EndsAt > start,
            cancellationToken);
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
