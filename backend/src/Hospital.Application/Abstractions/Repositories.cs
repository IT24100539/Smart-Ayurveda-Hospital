using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Abstractions;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(Patient patient, CancellationToken cancellationToken);
}

public interface IStaffUserRepository
{
    Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
        DateOnly? onDate,
        Guid? patientId,
        Guid? doctorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(Guid doctorId, DateTimeOffset start, DateTimeOffset end, Guid? excludeId, CancellationToken cancellationToken);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
}

public interface ITreatmentRepository
{
    Task<Treatment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Treatment?> GetByIdWithScheduleAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Treatment> Items, int Total)> SearchAsync(
        string? name,
        TreatmentCategory? category,
        bool? activeOnly,
        int page,
        int pageSize,
        string? sort,
        CancellationToken cancellationToken);
    Task AddAsync(Treatment treatment, CancellationToken cancellationToken);
    Task<TreatmentSchedule?> GetScheduleEntryAsync(Guid treatmentId, Guid entryId, CancellationToken cancellationToken);
    Task AddScheduleAsync(TreatmentSchedule entry, CancellationToken cancellationToken);
    void RemoveSchedule(TreatmentSchedule entry);
    Task<Therapist?> GetTherapistByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
