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

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await _db.Users.AddAsync(user, cancellationToken);
}

public sealed class StaffUserRepository : IStaffUserRepository
{
    private readonly HospitalDbContext _db;

    public StaffUserRepository(HospitalDbContext db) => _db = db;

    public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.StaffUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.StaffUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
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

public sealed class TreatmentRepository : ITreatmentRepository
{
    private readonly HospitalDbContext _db;

    public TreatmentRepository(HospitalDbContext db) => _db = db;

    public Task<Treatment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Treatments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Treatment?> GetByIdWithScheduleAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Treatments
            .Include(x => x.Schedules)
            .ThenInclude(s => s.Therapist)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Treatment> Items, int Total)> SearchAsync(
        string? name,
        TreatmentCategory? category,
        bool? activeOnly,
        int page,
        int pageSize,
        string? sort,
        CancellationToken cancellationToken)
    {
        var q = _db.Treatments.AsNoTracking()
            .Include(x => x.Schedules)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(term) || x.NameSinhala.ToLower().Contains(term));
        }

        if (category is not null)
        {
            q = q.Where(x => x.Category == category);
        }

        if (activeOnly == true)
        {
            q = q.Where(x => x.IsActive);
        }

        q = ApplySort(q, sort);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Treatment treatment, CancellationToken cancellationToken) =>
        await _db.Treatments.AddAsync(treatment, cancellationToken);

    public Task<TreatmentSchedule?> GetScheduleEntryAsync(Guid treatmentId, Guid entryId, CancellationToken cancellationToken) =>
        _db.TreatmentSchedules
            .Include(x => x.Therapist)
            .FirstOrDefaultAsync(x => x.Id == entryId && x.TreatmentId == treatmentId, cancellationToken);

    public async Task AddScheduleAsync(TreatmentSchedule entry, CancellationToken cancellationToken) =>
        await _db.TreatmentSchedules.AddAsync(entry, cancellationToken);

    public void RemoveSchedule(TreatmentSchedule entry) =>
        _db.TreatmentSchedules.Remove(entry);

    public Task<Therapist?> GetTherapistByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Therapists.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static IQueryable<Treatment> ApplySort(IQueryable<Treatment> query, string? sort)
    {
        var descending = false;
        var field = "name";
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var raw = sort.Trim();
            if (raw.StartsWith('-'))
            {
                descending = true;
                raw = raw[1..];
            }
            else if (raw.EndsWith(":desc", StringComparison.OrdinalIgnoreCase))
            {
                descending = true;
                raw = raw[..^5];
            }
            else if (raw.EndsWith(":asc", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw[..^4];
            }

            field = raw.Trim().ToLowerInvariant();
        }

        return (field, descending) switch
        {
            ("category", false) => query.OrderBy(x => x.Category).ThenBy(x => x.Name),
            ("category", true) => query.OrderByDescending(x => x.Category).ThenBy(x => x.Name),
            ("unitprice" or "price", false) => query.OrderBy(x => x.UnitPrice).ThenBy(x => x.Name),
            ("unitprice" or "price", true) => query.OrderByDescending(x => x.UnitPrice).ThenBy(x => x.Name),
            ("durationminutes" or "duration", false) => query.OrderBy(x => x.DurationMinutes).ThenBy(x => x.Name),
            ("durationminutes" or "duration", true) => query.OrderByDescending(x => x.DurationMinutes).ThenBy(x => x.Name),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
            (_, true) => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };
    }
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly HospitalDbContext _db;

    public EfUnitOfWork(HospitalDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
