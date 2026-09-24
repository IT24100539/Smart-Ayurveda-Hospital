using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class AppointmentServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenDoctorSlotTaken_ThrowsConflict()
    {
        var doctor = new StaffUser { Id = Guid.NewGuid(), Role = StaffRole.Doctor, FullName = "Dr. Rao" };
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair", Uhid = "SAH-2026-00002" };
        var appointments = new FakeAppointmentRepository { Overlap = true };
        var sut = new AppointmentService(
            appointments,
            new FakeStaffAndPatients(patient, doctor),
            new FakeStaffAndPatients(patient, doctor),
            new FakeUnitOfWork(),
            new FixedClock());

        var request = new CreateAppointmentRequest(
            patient.Id,
            doctor.Id,
            DateTimeOffset.UtcNow.AddDays(1),
            30,
            "Follow-up",
            null);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenAssigneeIsNotDoctor_Throws()
    {
        var therapist = new StaffUser { Id = Guid.NewGuid(), Role = StaffRole.Therapist, FullName = "Therapist" };
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair" };
        var sut = new AppointmentService(
            new FakeAppointmentRepository(),
            new FakeStaffAndPatients(patient, therapist),
            new FakeStaffAndPatients(patient, therapist),
            new FakeUnitOfWork(),
            new FixedClock());

        var request = new CreateAppointmentRequest(
            patient.Id,
            therapist.Id,
            DateTimeOffset.UtcNow.AddDays(1),
            30,
            "Abhyanga",
            null);

        await Assert.ThrowsAsync<DomainException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 9, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public bool Overlap { get; set; }

        public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Appointment?>(null);

        public Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
            DateOnly? onDate, Guid? patientId, Guid? doctorId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Appointment>)Array.Empty<Appointment>(), 0));

        public Task<bool> HasOverlapAsync(
            Guid doctorId, DateTimeOffset start, DateTimeOffset end, Guid? excludeId, CancellationToken cancellationToken) =>
            Task.FromResult(Overlap);

        public Task AddAsync(Appointment appointment, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStaffAndPatients : IPatientRepository, IStaffUserRepository
    {
        private readonly Patient _patient;
        private readonly StaffUser _staff;

        public FakeStaffAndPatients(Patient patient, StaffUser staff)
        {
            _patient = patient;
            _staff = staff;
        }

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == _patient.Id ? _patient : null);

        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult<Patient?>(null);

        Task<Patient?> IPatientRepository.GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<Patient?>(null);

        public Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(
            string? query, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Patient>)Array.Empty<Patient>(), 0));

        public Task AddAsync(Patient patient, CancellationToken cancellationToken) => Task.CompletedTask;

        Task<StaffUser?> IStaffUserRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == _staff.Id ? _staff : null);

        public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<StaffUser?>(null);

        public Task<StaffUser?> FindActiveByRoleAsync(StaffRole role, CancellationToken cancellationToken) =>
            Task.FromResult(_staff.IsActive && _staff.Role == role ? _staff : null);

        public Task<IReadOnlyList<StaffUser>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_staff.IsActive
                ? (IReadOnlyList<StaffUser>)new[] { _staff }
                : Array.Empty<StaffUser>());
    }
}
