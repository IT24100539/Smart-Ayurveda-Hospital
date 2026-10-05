using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class DoctorRepository : IDoctorRepository
{
    private readonly HospitalDbContext _db;

    public DoctorRepository(HospitalDbContext db) => _db = db;

    public Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Doctors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Doctor> Items, int Total)> ListAsync(
        string? query,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var doctors = _db.Doctors.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            doctors = doctors.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            doctors = doctors.Where(x =>
                x.Name.ToLower().Contains(term) || x.Specialty.ToLower().Contains(term));
        }

        var total = await doctors.CountAsync(cancellationToken);
        var items = await doctors
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken) =>
        await _db.Doctors.AddAsync(doctor, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DoctorRatingSummary>> GetRatingsAsync(
        IReadOnlyCollection<Guid> doctorIds,
        CancellationToken cancellationToken)
    {
        if (doctorIds.Count == 0)
        {
            return new Dictionary<Guid, DoctorRatingSummary>();
        }

        var rows = await _db.Feedbacks.AsNoTracking()
            .Where(feedback => feedback.Status == FeedbackStatus.Visible && feedback.AppointmentId != null)
            .Join(
                _db.Appointments.AsNoTracking(),
                feedback => feedback.AppointmentId,
                appointment => (Guid?)appointment.Id,
                (feedback, appointment) => new
                {
                    feedback.Rating,
                    appointment.DoctorId,
                    appointment.Status
                })
            .Where(row => row.Status == AppointmentStatus.Completed
                && row.DoctorId != null
                && doctorIds.Contains(row.DoctorId.Value))
            .Select(row => new { DoctorId = row.DoctorId!.Value, row.Rating })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.DoctorId)
            .ToDictionary(
                group => group.Key,
                group => new DoctorRatingSummary(
                    Math.Round((decimal)group.Average(x => x.Rating), 2, MidpointRounding.AwayFromZero),
                    group.Count()));
    }
}
